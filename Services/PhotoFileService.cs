using Microsoft.Extensions.Options;
using PhotoArchive.Models;
using SkiaSharp;

namespace PhotoArchive.Services
{
    public interface IPhotoFileService
    {
        /// <summary>Absolute folder that holds the original scans.</summary>
        string OriginalsRoot { get; }

        /// <summary>Absolute folder that holds cached thumbnails.</summary>
        string ThumbnailsRoot { get; }

        /// <summary>URL of the original image, or the placeholder if the file is missing.</summary>
        string OriginalUrl(Photo photo);

        /// <summary>URL of the thumbnail; generates it on first use. Returns the placeholder if the original is missing.</summary>
        string ThumbnailUrl(Photo photo);

        /// <summary>Generates the thumbnail if it is missing. Returns false if the original is missing or unreadable.</summary>
        bool EnsureThumbnail(Photo photo);
    }

    /// <summary>
    /// Resolves photo files on disk and generates thumbnails with SkiaSharp (cross-platform).
    /// Replaces the old System.Drawing-based Photo.Miniature().
    /// </summary>
    public class PhotoFileService : IPhotoFileService
    {
        public const string PlaceholderUrl = "/images/No_image.JPG";

        private readonly StorageOptions _options;
        private readonly ILogger<PhotoFileService> _logger;

        public string OriginalsRoot { get; }
        public string ThumbnailsRoot { get; }

        public PhotoFileService(IOptions<StorageOptions> options, IWebHostEnvironment env, ILogger<PhotoFileService> logger)
        {
            _options = options.Value;
            _logger = logger;
            OriginalsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.OriginalsRoot));
            ThumbnailsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.ThumbnailsRoot));
            Directory.CreateDirectory(OriginalsRoot);
            Directory.CreateDirectory(ThumbnailsRoot);
        }

        public string OriginalUrl(Photo photo)
        {
            var path = OriginalPath(photo);
            if (path == null || !File.Exists(path))
                return PlaceholderUrl;

            return StorageOptions.OriginalsRequestPath + "/"
                + Uri.EscapeDataString(photo.Film!.FolderName) + "/"
                + Uri.EscapeDataString(photo.Pic!);
        }

        public string ThumbnailUrl(Photo photo)
        {
            return EnsureThumbnail(photo)
                ? StorageOptions.ThumbnailsRequestPath + "/" + ThumbnailFileName(photo)
                : PlaceholderUrl;
        }

        public bool EnsureThumbnail(Photo photo)
        {
            var thumbPath = Path.Combine(ThumbnailsRoot, ThumbnailFileName(photo));
            if (File.Exists(thumbPath))
                return true;

            var originalPath = OriginalPath(photo);
            if (originalPath == null || !File.Exists(originalPath))
                return false; // don't cache a placeholder: the original may be copied in later

            try
            {
                using var source = SKBitmap.Decode(originalPath);
                if (source == null)
                {
                    _logger.LogWarning("Cannot decode image {Path}", originalPath);
                    return false;
                }

                var size = _options.ThumbnailSize;
                var scale = (float)size / Math.Max(source.Width, source.Height);
                var width = Math.Max(1, (int)(source.Width * scale));
                var height = Math.Max(1, (int)(source.Height * scale));

                using var resized = source.Resize(new SKImageInfo(width, height),
                    new SKSamplingOptions(SKCubicResampler.Mitchell));
                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

                // write to a temp file first so concurrent requests never see a half-written thumbnail
                var tempPath = thumbPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = File.Create(tempPath))
                    data.SaveTo(stream);
                File.Move(tempPath, thumbPath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Thumbnail generation failed for photo {Id} ({Path})", photo.Id, originalPath);
                return false;
            }
        }

        private static string ThumbnailFileName(Photo photo) => photo.Id + ".jpg";

        /// <summary>Full path of the original, or null if it can't be resolved or escapes the originals root.</summary>
        private string? OriginalPath(Photo photo)
        {
            if (photo.Film == null || string.IsNullOrEmpty(photo.Film.FolderName) || string.IsNullOrEmpty(photo.Pic))
                return null;

            var path = Path.GetFullPath(Path.Combine(OriginalsRoot, photo.Film.FolderName, photo.Pic));
            var root = OriginalsRoot.EndsWith(Path.DirectorySeparatorChar) ? OriginalsRoot : OriginalsRoot + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
    }
}
