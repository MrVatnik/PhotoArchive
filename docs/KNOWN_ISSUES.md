# Known issues and tech debt (review of Nov 2026)

The build was checked with .NET SDK 8.0.101: **0 errors**, with about 60 warnings. Most are nullable warnings (CS8602), plus CA1416 because `System.Drawing` works only on Windows.

Severity: 🔴 bug or data risk · 🟠 broken or unfinished feature · 🟡 tech debt · ✅ fixed

> **Phase 1 update (Nov 2026):** the app now runs on .NET 10. Items marked ✅ are fixed or partly fixed; everything else is still open.

## Bugs

| # | Sev | Where | Issue |
|---|-----|-------|-------|
| 1 | 🔴 | `PhotosController.AddToFilmByFile` | It filters `Photos.Where(p => p.Id == Id)`, where `Id` is the **film** id, but it should use `p.FilmId == Id`. The suggested "next page and line" is therefore usually computed from the whole archive, or from an unrelated photo. |
| 2 | 🔴 / ✅ partly | `AddToFilmByFiles` | The upload still saves only the file names (open). The placeholder is no longer cached, so thumbnails appear once the originals are copied in (fixed). |
| 3 | ✅ | `Photo.Miniature()` | Fixed: replaced by `PhotoFileService`, built on SkiaSharp. It disposes everything, handles a missing `Film`, and writes atomically. |
| 4 | 🔴 | All `Delete` actions | Cascade delete removes whole subtrees (see DATA_MODEL.md) and leaves orphaned files on disk. |
| 5 | 🟠 | `PhotosController.Page` | The lines come from dictionary insertion order and **are not sorted** by line number. |
| 6 | 🟠 | Bulk upload | `Place_In_Line` is always 0, so the order within a line is undefined. |
| 7 | 🟠 | Thumbnails | They are keyed by `Photo.Id` and never invalidated. If you edit `Pic` or `FolderName`, the old thumbnail stays. |
| 8 | 🟠 | `Program.cs` / `_Layout` | `/Home/Error` and `Home/Privacy` point to a `HomeController` that doesn't exist. |
| 9 | 🟠 | `Photo.Maximized()` | Removed, because it was never implemented. A web-size preview is still wanted (Phase 2). |
| 10 | ✅ | Paths | Fixed: paths come from configuration via `Path.Combine`, URLs are encoded, and `wwwroot/images` is referenced in lowercase. |

## Security

| # | Sev | Issue |
|---|-----|-------|
| 11 | 🔴 | There is no authentication. Anyone who can reach the port can edit or delete the whole archive. |
| 12 | 🟠 | `AddToFilmByFiles` has no `[HttpPost]` and no `[ValidateAntiForgeryToken]`. `Like` changes state over GET, which makes it open to CSRF. |
| 13 | 🟡 | Forms buffer up to 1 GB in memory (`BufferBody = true`), so a DoS is possible. |

## Tech debt

| # | Issue |
|---|-------|
| 14 | ✅ Upgraded to .NET 10 and EF Core 10.0.15 (LTS). |
| 15 | ✅ Replaced by EF Core migrations, applied at startup. v1.0 databases are baselined automatically. |
| 16 | ✅ Replaced by SkiaSharp, with Linux native assets included. Not yet tested in a container. |
| 17 | Thumbnails are still generated synchronously during view rendering. The first `/Photos` load on the restored archive (760 photos) took about 90 s. Parallel writes are now safe. |
| 18 | `Photos/Index` loads every photo with five `Include`s and has no paging. |
| 19 | The same five-`Include` query and `SelectList` setup are copy-pasted about 15 times. Dead `if (p == null)` checks follow `new Photo()`. |
| 20 | `ViewBag` and `ViewData` are used instead of view models, so there's no compile-time checking in the views. |
| 21 | Ordering dropdowns by usage runs a correlated `COUNT` subquery for each row. That's fine at small scale. |
| 22 | There are no tests, no CI and no README (until now). |
| 23 | Naming is inconsistent (`Is_Liked`, `Place_In_Line`, `To_String`, `Min/Sec`). |
