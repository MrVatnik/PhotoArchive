using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotoArchive.Data;
using PhotoArchive.Models;

namespace PhotoArchive.Services.Import
{
    /// <summary>One reviewed row of the import form.</summary>
    public class ImportRow
    {
        public bool Include { get; set; }
        public string Folder { get; set; } = "";
        public string? ImageFolder { get; set; }
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
    }

    public class ImportReport
    {
        public int Films { get; set; }
        public int Photos { get; set; }
        /// <summary>Reference data created on the way, as (kind, name).</summary>
        public List<(string Kind, string Name)> Created { get; } = new();
        public List<string> Skipped { get; } = new();
        public List<string> Errors { get; } = new();

        public string Summary()
        {
            var text = $"Imported {Films} film(s) with {Photos} photo(s).";
            if (Created.Count > 0)
                text += " Created " + string.Join("; ", Created.GroupBy(c => c.Kind).Select(g =>
                    g.Key == "recipe" ? $"{g.Count()} recipe(s)" : $"{g.Count()} {g.Key}(s): {string.Join(", ", g.Select(x => x.Name))}")) + ".";
            if (Skipped.Count > 0)
                text += $" Skipped {Skipped.Count} already imported.";
            return text;
        }
    }

    /// <summary>
    /// Creates films and photos for reviewed import rows, creating missing formats, cameras, film types,
    /// developers and recipes on the way. Only writes to the database: files are never moved or renamed.
    /// </summary>
    public class ArchiveImporter
    {
        private readonly PhotoContext _db;
        private readonly IPhotoFileService _files;
        private readonly IThumbnailQueue _thumbnails;
        private readonly ImportOptions _options;
        private readonly ILogger<ArchiveImporter> _logger;

        public ArchiveImporter(PhotoContext db, IPhotoFileService files, IThumbnailQueue thumbnails,
            IOptions<ImportOptions> options, ILogger<ArchiveImporter> logger)
        {
            _db = db;
            _files = files;
            _thumbnails = thumbnails;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ImportReport> ImportAsync(IEnumerable<ImportRow> rows, CancellationToken ct = default)
        {
            var report = new ImportReport();
            foreach (var row in rows.Where(r => r.Include))
            {
                var error = Validate(row);
                if (error != null)
                {
                    report.Errors.Add($"{row.Folder}: {error}");
                    continue;
                }
                if (await AlreadyImportedAsync(row, ct))
                {
                    report.Skipped.Add(row.Folder); // e.g. the form was submitted twice
                    continue;
                }

                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var photoIds = await ImportRollAsync(row, report, ct);
                    await tx.CommitAsync(ct);
                    foreach (var id in photoIds)
                        _thumbnails.Enqueue(id);
                    report.Films++;
                    report.Photos += photoIds.Count;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    _logger.LogError(ex, "Import of {Folder} failed", row.Folder);
                    report.Errors.Add($"{row.Folder}: {ex.GetBaseException().Message}");
                }
            }
            return report;
        }

        private string? Validate(ImportRow row)
        {
            if (string.IsNullOrWhiteSpace(row.ImageFolder)) return "no image folder";
            if (row.Date == null) return "date is required";
            if (string.IsNullOrWhiteSpace(row.Camera)) return "camera is required";
            if (string.IsNullOrWhiteSpace(row.Stock)) return "film stock is required";
            if (row.Iso == null || row.Iso <= 0) return "ISO is required";
            if (string.IsNullOrWhiteSpace(row.Developer)) return "developer is required";
            if (row.Page < 0 || row.Line < 0) return "album page/line can't be negative";
            if (!Directory.Exists(ImageDir(row))) return "image folder not found";
            return null;
        }

        private Task<bool> AlreadyImportedAsync(ImportRow row, CancellationToken ct)
        {
            var top = row.ImageFolder!.Split('/')[0];
            return _db.Films.AnyAsync(f => f.FolderName == top || f.FolderName.StartsWith(top + "/"), ct);
        }

        private string ImageDir(ImportRow row) =>
            Path.GetFullPath(Path.Combine(_files.OriginalsRoot, row.ImageFolder!.Replace('/', Path.DirectorySeparatorChar)));

        private async Task<List<int>> ImportRollAsync(ImportRow row, ImportReport report, CancellationToken ct)
        {
            var dir = ImageDir(row);
            if (!dir.StartsWith(_files.OriginalsRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("image folder is outside the archive");


            var camera = await CameraAsync(row.Camera!.Trim(), row.Format, report, ct);
            var perLine = ImportCatalog.FramesPerLine(camera.Format?.Name ?? row.Format);
            var filmType = await FilmTypeAsync(row.Stock!.Trim(), row.Iso!.Value, row.Color, report, ct);
            var developer = await DeveloperAsync(row.Developer!.Trim(), report, ct);
            var recipe = await RecipeAsync(filmType, developer, row.Ei ?? row.Iso!.Value, row.Color, report, ct);

            var film = new Film
            {
                Date = row.Date!.Value,
                FolderName = row.ImageFolder!,
                CameraId = camera.Id,
                RecipeId = recipe.Id
            };
            _db.Films.Add(film);
            await _db.SaveChangesAsync(ct);

            var images = Directory.EnumerateFiles(dir)
                .Where(f => _files.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(Path.GetFileName)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var photos = new List<Photo>();
            int page = row.Page, line = Math.Max(row.Line, row.Page > 0 ? 1 : 0), place = 0;
            foreach (var name in images)
            {
                photos.Add(new Photo { FilmId = film.Id, Name = name, Pic = name, Page = page, Line = line, Place_In_Line = place });
                if (page == 0)
                {
                    place++; // not in the album: keep them in file order on page 0
                    continue;
                }
                // fill the album like the paper sleeves: frames per strip by format, strips per page
                if (++place >= perLine)
                {
                    place = 0;
                    if (++line > _options.LinesPerPage)
                    {
                        line = 1;
                        page++;
                    }
                }
            }
            _db.Photos.AddRange(photos);
            await _db.SaveChangesAsync(ct);
            return photos.Select(p => p.Id).ToList();
        }

        // ---- find or create reference data (matched like the scanner: case, spaces and look-alike letters ignored) ----

        private async Task<Camera> CameraAsync(string name, string? formatName, ImportReport report, CancellationToken ct)
        {
            var key = ImportCatalog.Key(name);
            var existing = (await _db.Cameras.Include(c => c.Format).ToListAsync(ct)).FirstOrDefault(c => ImportCatalog.Key(c.Name) == key);
            if (existing != null)
                return existing;

            var format = await FormatAsync(string.IsNullOrWhiteSpace(formatName) ? "35 мм" : formatName.Trim(), report, ct);
            var camera = new Camera { Name = name, FormatId = format.Id, Format = format };
            _db.Cameras.Add(camera);
            await _db.SaveChangesAsync(ct);
            report.Created.Add(("camera", $"{name} ({format.Name})"));
            return camera;
        }

        private async Task<Format> FormatAsync(string name, ImportReport report, CancellationToken ct)
        {
            var key = ImportCatalog.Key(name);
            var existing = (await _db.Formats.ToListAsync(ct)).FirstOrDefault(f => ImportCatalog.Key(f.Name) == key);
            if (existing != null)
                return existing;
            var format = new Format { Name = name };
            _db.Formats.Add(format);
            await _db.SaveChangesAsync(ct);
            report.Created.Add(("format", name));
            return format;
        }

        private async Task<FilmType> FilmTypeAsync(string name, decimal iso, bool color, ImportReport report, CancellationToken ct)
        {
            var key = ImportCatalog.Key(name);
            var existing = (await _db.FilmTypes.ToListAsync(ct)).FirstOrDefault(f => ImportCatalog.Key(f.Name) == key && f.ISO == iso);
            if (existing != null)
                return existing;
            var known = ImportCatalog.Stocks.FirstOrDefault(s => s.Name == name);
            var filmType = new FilmType { Name = name, ISO = iso, Color_Is_Possible = known?.Color ?? color };
            _db.FilmTypes.Add(filmType);
            await _db.SaveChangesAsync(ct);
            report.Created.Add(("film type", filmType.ToString()));
            return filmType;
        }

        private async Task<Developer> DeveloperAsync(string name, ImportReport report, CancellationToken ct)
        {
            var key = ImportCatalog.Key(name);
            var existing = (await _db.Developers.ToListAsync(ct)).FirstOrDefault(d => ImportCatalog.Key(d.Name) == key);
            if (existing != null)
                return existing;
            var developer = new Developer { Name = name };
            _db.Developers.Add(developer);
            await _db.SaveChangesAsync(ct);
            report.Created.Add(("developer", name));
            return developer;
        }

        private async Task<Recipe> RecipeAsync(FilmType filmType, Developer developer, decimal ei, bool color, ImportReport report, CancellationToken ct)
        {
            var existing = await _db.Recipes.FirstOrDefaultAsync(r =>
                r.FilmTypeId == filmType.Id && r.DeveloperId == developer.Id && r.EI == ei && r.Color == color, ct);
            if (existing != null)
                return existing;
            // development time isn't in the notes: 0:00 means "unknown", edit the recipe later
            var recipe = new Recipe { FilmTypeId = filmType.Id, DeveloperId = developer.Id, EI = ei, Color = color, Min = 0, Sec = 0 };
            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync(ct);
            recipe.FilmType = filmType;
            recipe.Developer = developer;
            report.Created.Add(("recipe", recipe.ToString()));
            return recipe;
        }
    }
}
