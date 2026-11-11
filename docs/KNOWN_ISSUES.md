# Known issues and tech debt (review of Nov 2026)

The build was checked with .NET SDK 8.0.101: **0 errors**, with about 60 warnings. Most are nullable warnings (CS8602), plus CA1416 because `System.Drawing` works only on Windows.

Severity: 🔴 bug or data risk · 🟠 broken or unfinished feature · 🟡 tech debt · ✅ fixed

> **Phase 1 update (Nov 2026):** the app now runs on .NET 10.
>
> **Phase 2 update (Nov 2026):** bugs #1–#10, #12, #13 and #17 are fixed. Items marked ✅ are fixed; everything else is still open.

## Bugs

| # | Sev | Where | Issue |
|---|-----|-------|-------|
| 1 | ✅ | `PhotosController.AddToFilmByFile` | Fixed: the suggestion uses the film's own photos (last page, next free line), falling back to the archive's last page. |
| 2 | ✅ | `AddToFilmByFiles` | Fixed: files are streamed into the film's folder, validated (type and decodability), deduplicated, and never overwrite existing originals. |
| 3 | ✅ | `Photo.Miniature()` | Fixed: replaced by `PhotoFileService`, built on SkiaSharp. It disposes everything, handles a missing `Film`, and writes atomically. |
| 4 | ✅ | All `Delete` actions | Fixed: reference data is Restrict and the UI blocks deleting it while it's in use. Film → Photo still cascades. Cached files are cleaned up; originals are deliberately kept. |
| 5 | ✅ | `PhotosController.Page` | Fixed: lines are sorted by number. |
| 6 | ✅ | Bulk upload | Fixed: `Place_In_Line` is numbered by file name, continuing after any photos already in that line. Older data with equal values is ordered by file name. |
| 7 | ✅ | Thumbnails | Fixed: cache files carry a stamp of path, size and modification time; stale ones are replaced. |
| 8 | ✅ | `Program.cs` / `_Layout` | Fixed: `HomeController` added (`Index`, `Error`); the dead Privacy link was removed. |
| 9 | ✅ | `Photo.Maximized()` | Replaced by the 2048 px preview (`/media/photo/{id}/preview`) shown on the photo page; clicking it opens the original. |
| 10 | ✅ | Paths | Fixed: paths come from configuration via `Path.Combine`, URLs are encoded, and `wwwroot/images` is referenced in lowercase. |

## Security

| # | Sev | Issue |
|---|-----|-------|
| 11 | 🔴 | There is no authentication. Anyone who can reach the port can edit or delete the whole archive. |
| 12 | ✅ | Fixed: the upload and `Like` are POST-only with antiforgery validation (GET `Like` returns 405). |
| 13 | ✅ | Fixed: `BufferBody` removed; uploads spool to temp files and are streamed to storage. The 1 GB request limit remains. |

## Tech debt

| # | Issue |
|---|-------|
| 14 | ✅ Upgraded to .NET 10 and EF Core 10.0.15 (LTS). |
| 15 | ✅ Replaced by EF Core migrations, applied at startup. v1.0 databases are baselined automatically. |
| 16 | ✅ Replaced by SkiaSharp, with Linux native assets included. Not yet tested in a container. |
| 17 | ✅ Thumbnails are rendered by `MediaController` on request and by a background queue (uploads + startup warm-up); views no longer touch the disk. |
| 18 | `Photos/Index` loads every photo with five `Include`s and has no paging. |
| 19 | The same five-`Include` query and `SelectList` setup are copy-pasted about 15 times. Dead `if (p == null)` checks follow `new Photo()`. |
| 20 | `ViewBag` and `ViewData` are used instead of view models, so there's no compile-time checking in the views. |
| 21 | Ordering dropdowns by usage runs a correlated `COUNT` subquery for each row. That's fine at small scale. |
| 22 | There are no tests, no CI and no README (until now). |
| 23 | Naming is inconsistent (`Is_Liked`, `Place_In_Line`, `To_String`, `Min/Sec`). |
| 24 | New: the single-photo Create/Edit forms still take a free-text `Pic` and don't check that the file exists or is unique in the film. |
| 25 | New: uploads accept JPEG/PNG/WebP only. TIFF scans would need a decoder other than SkiaSharp. The importer uses a roll's JPEG sub-folder when the top level holds only TIFFs. |
| 26 | ✅ Fixed: originals in nested folders (`roll/roll`, `roll/Export`) returned 404 because the whole folder path was escaped as one URL segment. This affected film 1017 too. |
| 27 | New: the importer creates recipes with development time 0:00 ("unknown"); fill them in on the Recipes page. |
