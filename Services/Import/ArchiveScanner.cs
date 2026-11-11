using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoArchive.Data;

namespace PhotoArchive.Services.Import
{
    public class ImportOptions
    {
        public const string SectionName = "Import";

        /// <summary>The paper album index (.docx); relative paths are resolved against Storage:OriginalsRoot.</summary>
        public string AlbumIndexFile { get; set; } = "Плёнки описание.docx";

        /// <summary>Name of the per-roll metadata file.</summary>
        public string MetadataFilePattern { get; set; } = "*.txt";

        /// <summary>Sub-folders that never hold the roll's images (editor sidecars, crops).</summary>
        public string[] IgnoredSubfolders { get; set; } = { "CaptureOne", "squares" };

        /// <summary>Strips per album page. Frames per strip depend on the format (see ImportCatalog.FramesPerLine).</summary>
        public int LinesPerPage { get; set; } = 7;
    }

    /// <summary>A roll folder that is not in the database yet, with everything that could be worked out about it.</summary>
    public class ImportCandidate
    {
        public string Folder { get; set; } = "";
        /// <summary>Folder that holds the images, relative to the originals root ("roll" or "roll/sub"). Null if none.</summary>
        public string? ImageFolder { get; set; }
        public int ImageCount { get; set; }
        public bool Include { get; set; }

        public DateTime? Date { get; set; }
        public bool Color { get; set; }
        public string? Camera { get; set; }
        public string? Format { get; set; }
        public string? Stock { get; set; }
        public decimal? Iso { get; set; }
        public decimal? Ei { get; set; }
        public string? Developer { get; set; }
        public int Page { get; set; }
        public int Line { get; set; }
        public string PlacementSource { get; set; } = "";

        public string? RawText { get; set; }
        public List<string> Notes { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> New { get; set; } = new();   // reference data the import would create
    }

    public class ScanResult
    {
        public List<ImportCandidate> Candidates { get; } = new();
        public List<(string Folder, string Reason)> Skipped { get; } = new();
        public string? AlbumIndexStatus { get; set; }
    }

    /// <summary>Finds roll folders that are not in the database and pre-fills an import row for each (read-only).</summary>
    public class ArchiveScanner
    {
        private readonly PhotoContext _db;
        private readonly IPhotoFileService _files;
        private readonly ImportOptions _options;

        public ArchiveScanner(PhotoContext db, IPhotoFileService files, IOptions<ImportOptions> options)
        {
            _db = db;
            _files = files;
            _options = options.Value;
        }

        public async Task<ScanResult> ScanAsync(CancellationToken ct = default)
        {
            var result = new ScanResult();
            var root = _files.OriginalsRoot;

            var registered = (await _db.Films.Select(f => f.FolderName).ToListAsync(ct))
                .Select(f => f.Replace('\\', '/').Split('/')[0])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var cameraFormats = await _db.Cameras.Select(c => new { c.Name, Format = c.Format!.Name }).ToListAsync(ct);
            var cameras = cameraFormats.Select(c => c.Name).ToList();
            var formats = await _db.Formats.Select(f => f.Name).ToListAsync(ct);
            var stocks = await _db.FilmTypes.Select(f => new { f.Name, f.ISO }).ToListAsync(ct);
            var developers = await _db.Developers.Select(d => d.Name).ToListAsync(ct);
            var usedSlots = (await _db.Photos.Where(p => p.Page > 0).Select(p => new { p.Page, p.Line }).Distinct().ToListAsync(ct))
                .Select(p => (p.Page, p.Line)).ToHashSet();
            var occupied = new HashSet<(int Page, int Line)>(usedSlots);   // grows as rows are placed

            var album = ReadAlbumIndex(root, result);
            // rolls already in the database occupy their album slots: don't hand those addresses out again
            var freeAlbumRolls = album.Where(r => !usedSlots.Contains((r.StartPage, r.StartLine))).ToList();

            foreach (var dir in Directory.EnumerateDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(dir);
                if (registered.Contains(name))
                    continue;
                if (!RollMetadataParser.IsRollFolder(name))
                {
                    result.Skipped.Add((name, "not a roll folder (expected yy-MM-dd_col|bw_Camera)"));
                    continue;
                }

                var txt = ReadMetadataFile(dir);
                var meta = RollMetadataParser.Parse(name, txt);
                var c = new ImportCandidate
                {
                    Folder = name,
                    Date = meta.Date,
                    Color = meta.Color ?? meta.StockColor ?? false,
                    Camera = meta.Camera,
                    Format = meta.CameraFormat ?? "35 мм",
                    Stock = meta.Stock,
                    Iso = meta.Iso,
                    Ei = meta.Ei,
                    Developer = meta.Developer,
                    RawText = meta.RawText,
                    Notes = meta.Notes,
                    Warnings = meta.Warnings,
                    Include = meta.Stock != null && meta.Iso != null
                };

                // existing camera: its format comes from the database
                var knownCamera = cameraFormats.FirstOrDefault(x => ImportCatalog.Key(x.Name) == ImportCatalog.Key(c.Camera));
                if (knownCamera != null)
                    c.Format = knownCamera.Format;

                (c.ImageFolder, c.ImageCount) = FindImageFolder(root, dir);
                if (c.ImageFolder == null)
                {
                    c.Warnings.Add("No JPEG/PNG/WebP images (TIFF only?) — export JPEGs first");
                    c.Include = false;
                }
                if (meta.IsRescan)
                {
                    c.Warnings.Add("Second scan of a roll (-RESCAN): import only one of the two");
                    c.Include = false;
                }

                // ISO missing from the notes: take it from the database if the stock exists with a single speed
                if (c.Iso == null && c.Stock != null)
                {
                    var known = stocks.Where(s => ImportCatalog.Key(s.Name) == ImportCatalog.Key(c.Stock)).Select(s => s.ISO).Distinct().ToList();
                    if (known.Count == 1)
                    {
                        c.Iso = known[0];
                        c.Ei ??= known[0];
                        c.Warnings.Remove("Film speed (ISO) unknown");
                        c.Include = c.ImageFolder != null && !meta.IsRescan;
                    }
                }
                if (c.Stock == null || c.Iso == null)
                    c.Include = false; // needs a film stock and speed before it can be imported

                PlaceInAlbum(c, meta, freeAlbumRolls, occupied, usedSlots);

                // what would be created
                if (c.Camera != null && !cameras.Any(x => ImportCatalog.Key(x) == ImportCatalog.Key(c.Camera)))
                {
                    c.New.Add("camera");
                    if (!formats.Any(x => ImportCatalog.Key(x) == ImportCatalog.Key(c.Format)))
                        c.New.Add("format");
                }
                if (c.Stock != null && !stocks.Any(x => ImportCatalog.Key(x.Name) == ImportCatalog.Key(c.Stock) && x.ISO == c.Iso))
                    c.New.Add("film type");
                if (c.Developer != null && !developers.Any(x => ImportCatalog.Key(x) == ImportCatalog.Key(c.Developer)))
                    c.New.Add("developer");

                result.Candidates.Add(c);
            }

            return result;
        }

        private IReadOnlyList<AlbumRoll> ReadAlbumIndex(string root, ScanResult result)
        {
            var path = Path.Combine(root, _options.AlbumIndexFile);
            if (!File.Exists(path))
            {
                result.AlbumIndexStatus = $"Album index not found ({_options.AlbumIndexFile})";
                return Array.Empty<AlbumRoll>();
            }
            try
            {
                var rolls = AlbumIndexReader.Read(path);
                result.AlbumIndexStatus = $"Album index: {rolls.Count} rolls read from {_options.AlbumIndexFile}";
                return rolls;
            }
            catch (Exception ex)
            {
                result.AlbumIndexStatus = $"Album index could not be read: {ex.Message}";
                return Array.Empty<AlbumRoll>();
            }
        }

        private void PlaceInAlbum(ImportCandidate c, RollMetadata meta, List<AlbumRoll> albumRolls,
            HashSet<(int Page, int Line)> occupied, HashSet<(int Page, int Line)> dbSlots)
        {
            if (meta.AlbumPage is int page)
            {
                c.Page = page;
                c.Line = 1;
                c.PlacementSource = $"стр {page} in .txt";
            }
            else if (c.Date != null && c.Stock != null && c.Camera != null)
            {
                // the album's date is the shooting date (sometimes with the development date in brackets),
                // the folder's is the scan date: allow a few weeks, and require the same film and camera
                var match = albumRolls
                    .Where(r => ImportCatalog.Key(RollMetadataParser.ParseStockText(r.Stock).Stock) == ImportCatalog.Key(c.Stock)
                             && ImportCatalog.Key(ImportCatalog.FindCamera(r.Camera)?.Name ?? r.Camera) == ImportCatalog.Key(c.Camera))
                    .Select(r => (Roll: r, Days: r.Dates.Count == 0 ? int.MaxValue : r.Dates.Min(d => Math.Abs((d - c.Date.Value).TotalDays))))
                    .Where(x => x.Days <= 31)
                    .OrderBy(x => x.Days)
                    .Select(x => x.Roll)
                    .FirstOrDefault();
                if (match != null)
                {
                    albumRolls.Remove(match);
                    c.Page = match.StartPage;
                    c.Line = match.StartLine;
                    c.PlacementSource = $"album index {match.Address}";
                }
            }

            if (c.Page == 0)
            {
                c.PlacementSource = "not in the album (page 0)";
                return;
            }

            var perLine = ImportCatalog.FramesPerLine(c.Format);
            var lines = Math.Max(1, (int)Math.Ceiling(c.ImageCount / (double)perLine));
            var fromTxt = meta.AlbumPage != null;

            // "стр N" only names the page: several small rolls (120 film) can share it, so start at the first free strip.
            // Album-index addresses are exact and are kept as they are.
            if (fromTxt && c.Include)
            {
                var start = Enumerable.Range(1, _options.LinesPerPage).FirstOrDefault(l => !occupied.Contains((c.Page, l)));
                if (start == 0)
                    c.Warnings.Add($"Album page {c.Page} is already full");
                else if (start != c.Line)
                {
                    c.Line = start;
                    c.Notes.Add($"Shares album page {c.Page} with another roll: starts at strip {start}");
                }
            }
            else if (c.Include && dbSlots.Contains((c.Page, c.Line)))
            {
                // only photos already in the database count: album-index rolls may legitimately share a strip
                c.Warnings.Add($"Album page {c.Page}, strip {c.Line} already has photos in the database");
            }

            if (c.Include)
            {
                int p = c.Page, l = c.Line;
                for (var i = 0; i < lines; i++)
                {
                    occupied.Add((p, l));
                    if (++l > _options.LinesPerPage) { l = 1; p++; }
                }
            }
            if (lines > _options.LinesPerPage)
                c.Notes.Add($"{c.ImageCount} images ({perLine} per strip) fill more than one album page; continues on page {c.Page + 1}");
        }

        private string? ReadMetadataFile(string dir)
        {
            var file = Directory.EnumerateFiles(dir, _options.MetadataFilePattern).OrderBy(f => f).FirstOrDefault();
            if (file == null)
                return null;
            var bytes = File.ReadAllBytes(file);
            try
            {
                return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                // old files may be Windows-1251
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1251).GetString(bytes);
            }
        }

        /// <summary>The roll folder itself if it has web images, otherwise its best sub-folder (TIFF scans + JPEG export).</summary>
        private (string? Relative, int Count) FindImageFolder(string root, string dir)
        {
            var top = CountImages(dir);
            if (top > 0)
                return (Path.GetFileName(dir), top);

            var name = Path.GetFileName(dir);
            var best = Directory.EnumerateDirectories(dir)
                .Where(d => !_options.IgnoredSubfolders.Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                .Select(d => (Dir: d, Count: CountImages(d)))
                .Where(x => x.Count > 0)
                .OrderByDescending(x => string.Equals(Path.GetFileName(x.Dir), name, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.Count)
                .FirstOrDefault();
            return best.Dir == null ? (null, 0) : (Path.GetRelativePath(root, best.Dir).Replace('\\', '/'), best.Count);
        }

        private int CountImages(string dir) =>
            Directory.EnumerateFiles(dir).Count(f => _files.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
    }
}
