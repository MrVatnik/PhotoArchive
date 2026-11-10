# PhotoArchive

A personal web app for cataloguing **analog (film) photography**: which film stock was shot in which camera, how it was developed (developer, EI, time) and the scanned frames of each roll, laid out in "album pages".

Built in August 2022 (v1.0, "mostly stable") with a thumbnail generator added in July 2023. Upgraded to .NET 10 in November 2026 (roadmap Phase 1).

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

The app stores only file names in the database. Originals are read from disk:

```
{Storage:OriginalsRoot}/{Film.FolderName}/{Photo.Pic}     → served at /media/originals/...
{Storage:ThumbnailsRoot}/{Photo.Id}.jpg                    → served at /media/thumbs/...
```

The defaults in `appsettings.json` are `App_Data/films` and `App_Data/thumbnails`; git ignores `App_Data/`. To point at your real archive without committing a machine path, copy `appsettings.Local.example.json` to `appsettings.Local.json` (git ignores it too) and set the path:

```json
{ "Storage": { "OriginalsRoot": "D:\\img\\Films" } }
```

A thumbnail is generated the first time a photo is shown. If the original is missing, the page shows a placeholder, and the placeholder is not cached.

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
