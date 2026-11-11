using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoArchive.Data;
using PhotoArchive.Services.Import;

namespace PhotoArchive.Controllers
{
    /// <summary>Imports roll folders from the archive that aren't in the database yet (review first, then import).</summary>
    public class ImportController : Controller
    {
        private readonly ArchiveScanner _scanner;
        private readonly ArchiveImporter _importer;
        private readonly PhotoContext _db;

        public ImportController(ArchiveScanner scanner, ArchiveImporter importer, PhotoContext db)
        {
            _scanner = scanner;
            _importer = importer;
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var scan = await _scanner.ScanAsync(ct);

            // suggestions for the free-text fields: what's in the database plus the known spellings
            ViewBag.Cameras = Merge(await _db.Cameras.Select(c => c.Name).ToListAsync(ct), ImportCatalog.Cameras.Select(c => c.Name));
            ViewBag.Formats = Merge(await _db.Formats.Select(f => f.Name).ToListAsync(ct), ImportCatalog.Cameras.Select(c => c.Format));
            ViewBag.Stocks = Merge(await _db.FilmTypes.Select(f => f.Name).ToListAsync(ct), ImportCatalog.Stocks.Select(s => s.Name));
            ViewBag.Developers = Merge(await _db.Developers.Select(d => d.Name).ToListAsync(ct), ImportCatalog.Developers.Select(d => d.Name));
            return View(scan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 20000)]
        public async Task<IActionResult> Index(List<ImportRow> rows, CancellationToken ct)
        {
            var selected = rows.Where(r => r.Include).ToList();
            if (selected.Count == 0)
            {
                TempData["ImportResult"] = "Nothing selected.";
                return RedirectToAction(nameof(Index));
            }

            var report = await _importer.ImportAsync(selected, ct);
            TempData["ImportResult"] = report.Summary();
            if (report.Errors.Count > 0)
                TempData["ImportErrors"] = string.Join("\n", report.Errors);
            return RedirectToAction(nameof(Index));
        }

        private static List<string> Merge(IEnumerable<string> db, IEnumerable<string> known) =>
            db.Concat(known)
              .GroupBy(ImportCatalog.Key)
              .Select(g => g.First())
              .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
              .ToList();
    }
}
