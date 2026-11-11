# Architecture

## Overview

PhotoArchive is a single-project ASP.NET Core MVC application (`PhotoArchive.csproj`, `net10.0`). It has no service layer: controllers talk straight to the EF Core `PhotoContext`. Views are mostly scaffolded CRUD pages, with a few hand-written screens added on top.

```
Browser ──► Controllers (7) ──► PhotoContext (EF Core) ──► SQL Server LocalDB
               │
               └──► Razor Views (URLs only)

MediaController  /media/photo/{id}/{thumb|preview} ──► IPhotoFileService ──► Storage:OriginalsRoot / ThumbnailsRoot (disk)
ThumbnailQueue   (BackgroundService: uploads + startup warm-up) ──┘
```

| Folder               | Contents |
|----------------------|----------|
| `Program.cs`         | DI setup, form limits (1 GB), en-US culture, migrations at startup, `/media/*` static file mappings, default route `Films/Index` |
| `Data/PhotoContext.cs` | `DbContext` with 7 `DbSet`s |
| `Data/DatabaseInitializer.cs` | Marks the baseline migration as applied for v1.0 databases, then runs `Migrate()` |
| `Migrations/`        | EF Core migrations (`InitialCreate` is the v1.0 schema) |
| `Models/`            | Entities |
| `Services/`          | `StorageOptions` (config), `PhotoFileService` (file URLs, uploads, thumbnails/previews), `ThumbnailQueue` (background rendering) |
| `Services/Import/`   | Archive importer: `ImportCatalog` (spelling aliases), `RollMetadataParser` (folder name + `.txt`), `AlbumIndexReader` (album `.docx`), `ArchiveScanner` (read-only scan), `ArchiveImporter` (writes films/photos) |
| `Controllers/`       | One controller per entity, scaffolded CRUD plus custom actions |
| `Views/`             | Razor views per controller, plus `Shared/_Layout.cshtml` |
| `wwwroot/js/site.js` | jQuery that sets image orientation classes after load |
| `wwwroot/css/site.css` | Gallery and thumbnail styles |

## Domain in one sentence

A **Film** (a roll) is shot in a **Camera** (which has a **Format**, e.g. 35mm or 120). It is developed by a **Recipe** (a **FilmType** pushed or pulled to an EI in a **Developer** for mm:ss). It contains **Photos**, which are placed on album **Pages** in **Lines**.

See [DATA_MODEL.md](DATA_MODEL.md).

## Controllers and custom actions

All controllers have the scaffolded `Index / Details / Create / Edit / Delete` actions. Additions:

| Controller | Extra actions | Purpose |
|------------|---------------|---------|
| `FilmsController` | `AddFilmFromRec(id)`, `AddFilmFromCam(id)` | Create a film with the recipe or camera preselected. Dropdowns are sorted by usage count |
| `PhotosController` | `AddPhotoToFilm(id)` | Create a single photo inside a film |
| | `AddToFilmByFile(id)` (GET) / `AddToFilmByFiles` (POST) | Bulk upload into a film: suggests the next free page and line, saves files to the film's folder, numbers `Place_In_Line` by file name, skips duplicates and non-images, queues thumbnails |
| | `Like(id)` | Toggle `Is_Liked` (POST with antiforgery token) |
| | `Page(num)` | Album page view: lines sorted by number, photos by `Place_In_Line` (then file name), links to ±3 neighbouring pages |
| `MediaController` | `Photo(id, kind)` | Serves `/media/photo/{id}/thumb` and `/preview`, rendering on a cache miss. ETag/304 support; the placeholder is sent with `no-store` |
| `HomeController` | `Index`, `Error` | `/` redirects to Films; `Error` is the production exception page |
| `ImportController` | `Index` GET/POST | Review screen for unregistered roll folders (pre-filled by `ArchiveScanner`), then import of the selected rows (`ArchiveImporter`, one transaction per roll) |
| `CamerasController` | `AddCamFromFormat(id)` | Create a camera from a format |
| `RecipesController` | `AddRecFromDev(id)`, `AddRecFromFT(id)` | Create a recipe from a developer or film type |
| All parents | `Delete` GET/POST | Shows the descendant counts. Reference data (Format, Camera, FilmType, Developer, Recipe) can only be deleted when nothing uses it; deleting a Film removes its photo records |

The parent `Details` pages list their children (for example, Film details show the photo grid and page links).

## Image pipeline

1. **Originals** reach `{Storage:OriginalsRoot}/{FolderName}/{Pic}` by upload (`SaveOriginalAsync`: streamed to a temp file, checked to be a decodable JPEG/PNG/WebP, then renamed; never overwrites) or by copying them in by hand.
2. **Views only emit URLs** (`PhotoFiles.ThumbnailUrl/PreviewUrl/OriginalUrl`, injected through `_ViewImports.cshtml`), so a page renders instantly whatever the cache state.
3. **`MediaController`** handles `/media/photo/{id}/thumb|preview`. `GetOrCreateAsync` looks for `{ThumbnailsRoot}/{kind}/{id}_{stamp}.jpg`, where `stamp` hashes the kind, size, relative path, file length and modification time. On a miss it renders with SkiaSharp: scaled JPEG decode, Mitchell resize (175 px thumb, 2048 px preview, never upscaled), EXIF 90/180/270° rotation, atomic write, and removal of stale stamps. A striped lock stops two requests from rendering the same photo.
4. **`ThumbnailQueue`** (a `Channel` plus `BackgroundService`) renders thumbnails after uploads and, on startup, for every photo, so galleries are normally served straight from the cache.
5. A missing or unreadable original gives `/images/No_image.JPG` with `Cache-Control: no-store`, so nothing is cached.
6. All paths are checked to stay inside `OriginalsRoot`, so a crafted `FolderName` or `Pic` can't reach other files.
7. `site.js` reveals each image (`.temp` → visible) as soon as it loads and adds `photo_hor` / `photo_ver_rot` / `photoBig*` classes from its natural size.
8. Deleting photos or films removes cached renditions only; **originals are never deleted by the app**.

## Archive import

1. **Scan (read-only).** Every sub-folder of `OriginalsRoot` that no film points at, and whose name matches `yy-MM-dd_col|bw_Camera`, becomes a row. Other folders are listed as skipped.
2. **Image folder.** The roll folder if it holds JPEG/PNG/WebP; otherwise its best sub-folder (same name as the roll first, then the most images), ignoring `CaptureOne` and `squares`. `Film.FolderName` can therefore be nested (`roll/Export`).
3. **Metadata.** `RollMetadataParser` reads the date and colour from the folder name, and film stock, ISO, EI, developer, camera, lens and `стр N` from the `.txt`. Names go through `ImportCatalog`, which matches ignoring case, spaces, punctuation and Cyrillic/Latin look-alikes. If no developer is given, colour rolls get lab C-41 (ECN-2 for motion-picture stocks) and B/W rolls get "BW in lab". EI comes from the notes, otherwise the box speed; on cameras with a limited speed dial (`ImportCatalog.EiSettings`: the LC-A has 25/50/100/200/400) it snaps to the nearest setting.
4. **Album placement.** `стр N` gives the page; rolls sharing a page start at the next free strip. Otherwise the album index (`.docx`) is matched on film, camera and a date within 31 days. Anything else goes to page 0 (not placed). Frames per strip depend on the format: 35 mm 6, 6x6 3, 6x9 2, sheet 1; 7 strips per page.
5. **Review.** Rows missing a film stock or ISO, `-RESCAN` duplicates and folders without images start unchecked. Every field can be edited, with suggestions from the database and the catalog.
6. **Import.** Per row, in one transaction: find or create the format, camera, film type, developer and recipe; create the Film and one Photo per image in file-name order, laid out strip by strip; queue thumbnails. Folders already imported are skipped, so submitting twice is harmless.

## Configuration

- Connection string `DefaultConnection` in `appsettings.json` points to `(localdb)\mssqllocaldb`, database `PhotoArchiveDB`. The SQL Server compatibility level is pinned to 150 (SQL Server 2019 / LocalDB).
- The `Storage` section holds `OriginalsRoot`, `ThumbnailsRoot`, `ThumbnailSize`, `PreviewSize` and `WarmUpThumbnailsOnStartup`. Relative paths resolve against the content root. Override them per machine in the git-ignored `appsettings.Local.json` (see `appsettings.Local.example.json`).
- Uploads are limited to 1 GB per request and aren't buffered in memory; ASP.NET spools large files to temp files.
- Culture is fixed to en-US, so decimals use `.` (this matters for ISO and EI).

## What's missing structurally

- No authentication or authorisation, although `UseAuthorization` is called.
- No tests and no CI.
