# Architecture

## Overview

PhotoArchive is a single-project ASP.NET Core MVC application (`PhotoArchive.csproj`, `net10.0`). It has no service layer: controllers talk straight to the EF Core `PhotoContext`. Views are mostly scaffolded CRUD pages, with a few hand-written screens added on top.

```
Browser ──► Controllers (7) ──► PhotoContext (EF Core) ──► SQL Server LocalDB
               │
               └──► Razor Views ──► IPhotoFileService ──► Storage:OriginalsRoot / ThumbnailsRoot (disk)
```

| Folder               | Contents |
|----------------------|----------|
| `Program.cs`         | DI setup, form limits (1 GB), en-US culture, migrations at startup, `/media/*` static file mappings, default route `Films/Index` |
| `Data/PhotoContext.cs` | `DbContext` with 7 `DbSet`s |
| `Data/DatabaseInitializer.cs` | Marks the baseline migration as applied for v1.0 databases, then runs `Migrate()` |
| `Migrations/`        | EF Core migrations (`InitialCreate` is the v1.0 schema) |
| `Models/`            | Entities |
| `Services/`          | `StorageOptions` (config) and `PhotoFileService` (file URLs and thumbnails) |
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
| | `AddToFilmByFile(id)` (GET) / `AddToFilmByFiles` (POST) | Bulk-create photos from a multi-file picker. **Only file names are saved**, not the files themselves |
| | `Like(id)` | Toggle `Is_Liked` (GET request) |
| | `Page(num)` | Album page view: photos grouped by `Line`, ordered by `Place_In_Line`, links to ±3 neighbouring pages |
| `CamerasController` | `AddCamFromFormat(id)` | Create a camera from a format |
| `RecipesController` | `AddRecFromDev(id)`, `AddRecFromFT(id)` | Create a recipe from a developer or film type |
| All parents | `Delete` GET | Shows how many descendants will be removed ("It contains N films, M photos") |

The parent `Details` pages list their children (for example, Film details show the photo grid and page links).

## Image pipeline

1. The originals are copied manually to `{Storage:OriginalsRoot}/{FolderName}/{Pic}`. The upload stores only file names (roadmap Phase 2 fixes this).
2. Views call `PhotoFiles.ThumbnailUrl(photo)`. `IPhotoFileService` is injected into every view through `_ViewImports.cshtml`:
   - if `{ThumbnailsRoot}/{Id}.jpg` exists, it returns `/media/thumbs/{Id}.jpg`;
   - otherwise it decodes the original with SkiaSharp, resizes it to `Storage:ThumbnailSize` (175 px) on the longest side, writes the JPEG atomically (temp file, then rename) and returns the URL;
   - if the original is missing or unreadable, it returns `/images/No_image.JPG` **without caching it**.
3. `Photos/Details` shows the original via `PhotoFiles.OriginalUrl(photo)`, and clicking it opens the full-size image in a new tab. The URL returns `/media/originals/{folder}/{file}` with each segment URL-encoded.
4. File paths are checked to stay inside `OriginalsRoot`, so a crafted `FolderName` or `Pic` can't read other files.
5. `site.js` adds `photo_hor` or `photo_ver_rot` classes based on the rendered aspect ratio.

## Configuration

- Connection string `DefaultConnection` in `appsettings.json` points to `(localdb)\mssqllocaldb`, database `PhotoArchiveDB`. The SQL Server compatibility level is pinned to 150 (SQL Server 2019 / LocalDB).
- The `Storage` section holds `OriginalsRoot`, `ThumbnailsRoot` and `ThumbnailSize`. Relative paths resolve against the content root. Override them per machine in the git-ignored `appsettings.Local.json` (see `appsettings.Local.example.json`).
- Forms buffer up to 1 GB in memory (`FormOptions.BufferBody = true`).
- Culture is fixed to en-US, so decimals use `.` (this matters for ISO and EI).

## What's missing structurally

- No authentication or authorisation, although `UseAuthorization` is called.
- No `HomeController`, so the error handler `/Home/Error` and the footer link "Privacy" are broken.
- No tests and no CI.
