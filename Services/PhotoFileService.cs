using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PhotoArchive.Models;
using SkiaSharp;

namespace PhotoArchive.Services
{
    /// <summary>Generated, cached renditions of an original.</summary>
    public enum ImageKind
    {
        /// <summary>Gallery thumbnail (Storage:ThumbnailSize).</summary>
        Thumb,
        /// <summary>Web preview for the photo page (Storage:PreviewSize).</summary>
        Preview
    }

    public enum UploadOutcome
    {
        Saved,
        AlreadyOnDisk,
        UnsupportedType,
        NotAnImage,
        InvalidFolder
    }

    public interface IPhotoFileService
    {
        /// <summary>Absolute folder that holds the original scans.</summary>
        string OriginalsRoot { get; }

        /// <summary>File extensions accepted by uploads (lower case, with dot).</summary>
        IReadOnlyCollection<string> SupportedExtensions { get; }

        /// <summary>URL of the original image, or the placeholder if the file is missing.</summary>
        string OriginalUrl(Photo photo);

        /// <summary>URL of the gallery thumbnail (generated on first request).</summary>
        string ThumbnailUrl(Photo photo);

        /// <summary>URL of the web-size preview (generated on first request).</summary>
        string PreviewUrl(Photo photo);

        /// <summary>Full path of the original, or null if it can't be resolved or escapes the originals root.</summary>
        string? OriginalPath(Photo photo);

        /// <summary>Path of the cached rendition, generating it if needed. Null if the original is missing or unreadable.</summary>
        Task<string?> GetOrCreateAsync(Photo photo, ImageKind kind, CancellationToken ct = default);

        /// <summary>Removes cached thumbnails/previews of a photo. Never touches the original.</summary>
        void DeleteCached(int photoId);

        /// <summary>Saves an uploaded file as {OriginalsRoot}/{film.FolderName}/{fileName}. Never overwrites.</summary>
        Task<UploadOutcome> SaveOriginalAsync(Film film, IFormFile file, CancellationToken ct = default);
    }

    /// <summary>
    /// Resolves photo files on disk, saves uploads, and generates thumbnails/previews with SkiaSharp.
    /// Cache files are named {photoId}_{stamp}.jpg, where the stamp changes whenever the original's
    /// path, size or modification time changes, so edits invalidate the cache automatically.
    /// </summary>
    public class PhotoFileService : IPhotoFileService
    {
        public const string PlaceholderUrl = "/images/No_image.JPG";

        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // striped locks so two requests never generate the same file at once
        private static readonly SemaphoreSlim[] Locks = Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        private readonly StorageOptions _options;
        private readonly ILogger<PhotoFileService> _logger;
        private readonly string _cacheRoot;

        public string OriginalsRoot { get; }
        public IReadOnlyCollection<string> SupportedExtensions => Extensions;

        public PhotoFileService(IOptions<StorageOptions> options, IWebHostEnvironment env, ILogger<PhotoFileService> logger)
        {
            _options = options.Value;
            _logger = logger;
            OriginalsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.OriginalsRoot));
            _cacheRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.ThumbnailsRoot));
            Directory.CreateDirectory(OriginalsRoot);
            foreach (var kind in Enum.GetValues<ImageKind>())
                Directory.CreateDirectory(CacheDir(kind));
        }

        public string OriginalUrl(Photo photo)
        {
            var path = OriginalPath(photo);
            if (path == null || !File.Exists(path))
                return PlaceholderUrl;

            // FolderName may be nested ("roll/roll" when JPEGs sit in a sub-folder): escape each segment
            var folder = string.Join("/", photo.Film!.FolderName.Split('/', '\\').Select(Uri.EscapeDataString));
            return StorageOptions.OriginalsRequestPath + "/" + folder + "/" + Uri.EscapeDataString(photo.Pic!);
        }

        public string ThumbnailUrl(Photo photo) => $"/media/photo/{photo.Id}/thumb";

        public string PreviewUrl(Photo photo) => $"/media/photo/{photo.Id}/preview";

        public string? OriginalPath(Photo photo)
        {
            if (photo.Film == null || string.IsNullOrEmpty(photo.Pic))
                return null;
            var folder = FolderPath(photo.Film);
            if (folder == null)
                return null;

            var path = Path.GetFullPath(Path.Combine(folder, photo.Pic));
            return IsInside(path, folder) ? path : null;
        }

        public async Task<string?> GetOrCreateAsync(Photo photo, ImageKind kind, CancellationToken ct = default)
        {
            var original = OriginalPath(photo);
            if (original == null)
                return null;
            var info = new FileInfo(original);
            if (!info.Exists)
                return null; // don't cache a placeholder: the original may be copied in later

            var size = kind == ImageKind.Thumb ? _options.ThumbnailSize : _options.PreviewSize;
            var stamp = Stamp($"{kind}|{size}|{photo.Film!.FolderName}/{photo.Pic}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
            var dir = CacheDir(kind);
            var target = Path.Combine(dir, $"{photo.Id}_{stamp}.jpg");
            if (File.Exists(target))
                return target;

            var gate = Locks[(photo.Id & 0x7fffffff) % Locks.Length];
            await gate.WaitAsync(ct);
            try
            {
                if (File.Exists(target))
                    return target;

                // CPU-bound; keep it off the request thread's synchronous path
                var ok = await Task.Run(() => Render(original, target, size), ct);
                if (!ok)
                    return null;

                foreach (var stale in Directory.EnumerateFiles(dir, $"{photo.Id}_*.jpg"))
                    if (!string.Equals(stale, target, StringComparison.OrdinalIgnoreCase))
                        TryDelete(stale);
                return target;
            }
            finally
            {
                gate.Release();
            }
        }

        public void DeleteCached(int photoId)
        {
            foreach (var kind in Enum.GetValues<ImageKind>())
                foreach (var file in Directory.EnumerateFiles(CacheDir(kind), $"{photoId}_*.jpg"))
                    TryDelete(file);
        }

        public async Task<UploadOutcome> SaveOriginalAsync(Film film, IFormFile file, CancellationToken ct = default)
        {
            var folder = FolderPath(film);
            if (folder == null)
                return UploadOutcome.InvalidFolder;

            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName) || !Extensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                return UploadOutcome.UnsupportedType;

            var target = Path.Combine(folder, fileName);
            if (!IsInside(Path.GetFullPath(target), folder))
                return UploadOutcome.UnsupportedType;
            if (File.Exists(target))
                return UploadOutcome.AlreadyOnDisk; // keep what is on disk, never overwrite an original

            Directory.CreateDirectory(folder);
            var temp = target + "." + Guid.NewGuid().ToString("N") + ".uploading";
            try
            {
                await using (var stream = File.Create(temp))
                    await file.CopyToAsync(stream, ct);

                using (var codec = SKCodec.Create(temp))
                {
                    if (codec == null)
                    {
                        TryDelete(temp);
                        return UploadOutcome.NotAnImage;
                    }
                }

                File.Move(temp, target, overwrite: false);
                return UploadOutcome.Saved;
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }

        // ---- helpers ----

        private string CacheDir(ImageKind kind) => Path.Combine(_cacheRoot, kind.ToString().ToLowerInvariant());

        /// <summary>Folder of a film, or null if FolderName is empty or would escape the originals root.</summary>
        private string? FolderPath(Film film)
        {
            if (string.IsNullOrWhiteSpace(film.FolderName))
                return null;
            var path = Path.GetFullPath(Path.Combine(OriginalsRoot, film.FolderName));
            return IsInside(path, OriginalsRoot) ? path : null;
        }

        private static bool IsInside(string path, string root)
        {
            var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Stamp(string input) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)), 0, 8).ToLowerInvariant();

        private bool Render(string originalPath, string targetPath, int maxSide)
        {
            try
            {
                using var codec = SKCodec.Create(originalPath);
                if (codec == null)
                {
                    _logger.LogWarning("Cannot decode image {Path}", originalPath);
                    return false;
                }

                using var decoded = DecodeScaled(codec, maxSide);
                if (decoded == null)
                    return false;

                var scale = Math.Min(1f, (float)maxSide / Math.Max(decoded.Width, decoded.Height));
                var width = Math.Max(1, (int)Math.Round(decoded.Width * scale));
                var height = Math.Max(1, (int)Math.Round(decoded.Height * scale));

                using var resized = scale < 1f
                    ? decoded.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell))
                    : decoded.Copy();
                using var oriented = ApplyOrigin(resized, codec.EncodedOrigin);
                using var image = SKImage.FromBitmap(oriented);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

                // write to a temp file first so concurrent readers never see a half-written file
                var temp = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = File.Create(temp))
                    data.SaveTo(stream);
                File.Move(temp, targetPath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rendering failed for {Path}", originalPath);
                return false;
            }
        }

        /// <summary>Decodes at a reduced size when the codec supports it (fast path for big JPEG scans).</summary>
        private static SKBitmap? DecodeScaled(SKCodec codec, int maxSide)
        {
            var info = codec.Info;
            var scale = (float)maxSide / Math.Max(info.Width, info.Height);
            if (scale < 1f)
            {
                var dims = codec.GetScaledDimensions(scale);
                if (Math.Max(dims.Width, dims.Height) >= maxSide)
                {
                    var scaled = SKBitmap.Decode(codec, info.WithSize(dims.Width, dims.Height));
                    if (scaled != null)
                        return scaled;
                }
            }
            return SKBitmap.Decode(codec, info);
        }

        /// <summary>Applies the common EXIF rotations (90/180/270); mirrored orientations are left as is.</summary>
        private static SKBitmap ApplyOrigin(SKBitmap src, SKEncodedOrigin origin)
        {
            if (origin != SKEncodedOrigin.RightTop && origin != SKEncodedOrigin.BottomRight && origin != SKEncodedOrigin.LeftBottom)
                return src.Copy();

            var swap = origin != SKEncodedOrigin.BottomRight;
            var dst = new SKBitmap(swap ? src.Height : src.Width, swap ? src.Width : src.Height);
            using var canvas = new SKCanvas(dst);
            switch (origin)
            {
                case SKEncodedOrigin.RightTop: // rotate 90° clockwise
                    canvas.Translate(dst.Width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.BottomRight: // rotate 180°
                    canvas.Translate(dst.Width, dst.Height);
                    canvas.RotateDegrees(180);
                    break;
                case SKEncodedOrigin.LeftBottom: // rotate 90° counter-clockwise
                    canvas.Translate(0, dst.Height);
                    canvas.RotateDegrees(-90);
                    break;
            }
            canvas.DrawBitmap(src, 0, 0);
            return dst;
        }

        private void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete {Path}", path); }
        }
    }
}
