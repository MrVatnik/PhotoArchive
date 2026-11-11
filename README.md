# PhotoArchive

A personal web app for cataloguing **analog (film) photography**: which film stock was shot in which camera, how it was developed (developer, EI, time) and the scanned frames of each roll, laid out in "album pages".

Built in August 2022 (v1.0, "mostly stable") with a thumbnail generator added in July 2023. Upgraded to .NET 10 in November 2026 (roadmap Phase 1), then stabilised: real uploads, background thumbnails, safe deletes (Phase 2).

## Tech stack

| Layer      | Technology                                             |
|------------|--------------------------------------------------------|
| Runtime    | .NET 10 (LTS, supported until Nov 2028)                |
| Web        | ASP.NET Core MVC, Razor views, Bootstrap 5, jQuery     |
| Data       | EF Core 10 + SQL Server LocalDB, with schema migrations |
| Images     | SkiaSharp (cross-platform) for thumbnails              |

## Running locally

Prerequisites: the .NET 10 SDK, and SQL Server LocalDB (it comes with Visual Studio).

```bash
dotnet run
```

Open https://localhost:7265. On startup the app applies EF Core migrations, creating the `PhotoArchiveDB` database if it doesn't exist.

### Restoring an old backup

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "RESTORE DATABASE PhotoArchiveDB FROM DISK='D:\path\PhotoArchiveDBReserve.bak' WITH MOVE 'PhotoArchiveDB' TO '%USERPROFILE%\PhotoArchiveDB.mdf', MOVE 'PhotoArchiveDB_log' TO '%USERPROFILE%\PhotoArchiveDB_log.ldf'"
```

A database created by v1.0 has no migration history. On first start the app detects this and marks the baseline migration `InitialCreate` as applied, so no data is touched.

### Where photos live

The database stores file names; the files themselves live on disk:

```
{Storage:OriginalsRoot}/{Film.FolderName}/{Photo.Pic}          → /media/originals/{folder}/{file}
{Storage:ThumbnailsRoot}/thumb/{Photo.Id}_{stamp}.jpg          → /media/photo/{id}/thumb    (175 px)
{Storage:ThumbnailsRoot}/preview/{Photo.Id}_{stamp}.jpg        → /media/photo/{id}/preview  (2048 px)
```

**Film → Add by files** saves the uploaded files into the film's folder (JPEG, PNG or WebP). Files already in the film are skipped, files already in the folder are linked rather than overwritten, and anything that isn't a readable image is rejected.

The defaults in `appsettings.json` are `App_Data/films` and `App_Data/thumbnails`; git ignores `App_Data/`. To point at your real archive without committing a machine path, copy `appsettings.Local.example.json` to `appsettings.Local.json` (git ignores it too) and set the path. Environment variables (`Storage__OriginalsRoot`) still override it:

```json
{ "Storage": { "OriginalsRoot": "D:\\img\\Films" } }
```

Thumbnails and previews are generated on first request and in the background: after an upload, and for every photo when the app starts (`Storage:WarmUpThumbnailsOnStartup`). The `{stamp}` changes when the original's path, size or date changes, so edited files get fresh thumbnails. If the original is missing, the page shows a placeholder, and the placeholder is not cached.

**Deleting never removes originals.** Deleting a film removes its photo records and cached thumbnails only. Formats, cameras, film types, developers and recipes can't be deleted while something still uses them.

### Importing existing rolls

**Import** (top menu) lists every roll folder under `OriginalsRoot` that isn't in the database yet, pre-filled from the folder name, its `Пленка+Аппарат.txt` and the paper album index (`Плёнки описание.docx`); see [docs/ARCHIVE_SOURCES.md](docs/ARCHIVE_SOURCES.md). Review the rows, fix anything that's wrong, and import. Missing cameras, formats, film types, developers and recipes are created (recipes with development time 0:00, meaning unknown). The importer only writes to the database: files are never moved or renamed. Settings live in the `Import` section (`AlbumIndexFile`, `IgnoredSubfolders`, `LinesPerPage`).

> Upgrading from Phase 1: the old `App_Data/thumbnails/{id}.jpg` files are no longer used and can be deleted (the new cache lives in `thumb/` and `preview/` subfolders).

### Tests

```bash
dotnet test
```

`tests/PhotoArchive.Tests` holds unit tests for the importer (every tricky line from the real `.txt` files) and integration tests that start the whole app in-process against a temporary SQLite database and temp folders, so they never touch SQL Server or the real archive. GitHub Actions (`.github/workflows/ci.yml`) builds and runs them on Ubuntu and Windows for every pull request and push to `master`.

### Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name>
```

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): how the app is put together, request flows, and screens
- [docs/DATA_MODEL.md](docs/DATA_MODEL.md): the entities, their relations and cascade-delete behaviour
- [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md): bugs and technical debt found during the 2026 review
- [docs/ROADMAP.md](docs/ROADMAP.md): proposed plan to finish and upgrade the app
- [docs/ARCHIVE_SOURCES.md](docs/ARCHIVE_SOURCES.md): how the existing photo archive is organised (folders, `.txt` metadata, the album `.docx`), as input for the importer
