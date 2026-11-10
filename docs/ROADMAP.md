# Roadmap: finishing and upgrading PhotoArchive

Guiding idea: **keep the domain, replace the plumbing.** The data model (Format → Camera → Film ← Recipe ← FilmType/Developer, with Photos on album pages) is the valuable part. Most of the effort should go into making the images trustworthy and the platform current, not into a rewrite.

Issue numbers (#n) refer to [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

---

## Phase 0: Safety net (½ day)

- Back up `PhotoArchiveDB` (a `.bak`, or a script export) and the `wwwroot/Images/Films` folder.
- Tag the current state: `git tag v1.0-legacy`.
- Optional: add a tiny `dotnet test` project with a smoke test that the app starts and `/Films` returns 200.

## Phase 1: Platform upgrade ✅ done (Nov 2026)

> Done: `net10.0`, EF Core 10.0.15, an `InitialCreate` migration with automatic baseline for v1.0 databases, SkiaSharp thumbnails behind `IPhotoFileService`, and storage paths from configuration. It was verified against the restored 2023 backup (22 films, 760 photos, 759 originals found). It stays on SQL Server LocalDB, and the move to SQLite is deferred. **Not done:** a Linux container test, because Docker isn't installed.

Do this before fixing bugs. It's mechanical, and every later fix then lands on the supported stack.

1. Retarget `net6.0` to **`net10.0`** (the current LTS, supported until Nov 2028). Bump EF Core and other packages to 10.x. Remove `Microsoft.VisualStudio.Web.CodeGeneration.Design` unless you scaffold.
2. **Switch to EF Core migrations.** Remove `EnsureCreated()` from the constructor. Create a baseline migration that matches the existing schema (`dotnet ef migrations add Initial`, then mark it as applied on the existing DB). After that, apply migrations at startup or with `dotnet ef database update`.
3. **Replace `System.Drawing`** with a cross-platform library (#16). The options:
   - **SkiaSharp**: MIT licence, fast. *Recommended.*
   - **NetVips**: very fast and low on memory for big scans.
   - **ImageSharp**: a nice API, but it uses the Six Labors Split License (free for open source or small revenue, so check that it fits).
4. Normalise paths: one config value `Storage:RootPath`, `Path.Combine` everywhere, and lowercase folder names (#10).
5. Consider **SQLite** in place of LocalDB if this is a personal, single-user app. It's one file, easy to back up and works on any OS. EF makes the switch cheap. Keep SQL Server if you want it.

**Exit criterion:** the app runs on .NET 10 on Windows *and* in a Linux container, with existing data intact.

## Phase 2: Stabilise ✅ done (Nov 2026)

> Done: real streaming uploads with validation; on-demand thumbnail (175 px) and preview (2048 px) endpoint with self-invalidating cache and ETags; background queue with startup warm-up; page/line fixes; `HomeController`; POST + antiforgery for Like and upload; Restrict deletes (migration `RestrictReferenceDeletes`). Verified with an end-to-end script against a copy of the 2023 backup.
>
> Deviations from the plan below: (a) deleting a film **keeps the original files**, because `OriginalsRoot` is the photographer's real archive (most folders there aren't in the database); only cached renditions are removed. (b) `Like` is a plain POST form (works without JS) rather than `fetch()`. (c) Thumbnails and previews are served from `/media/photo/{id}/thumb|preview` instead of `/thumb/{id}?size=`.

Fix the bugs in KNOWN_ISSUES.md, in this order:

1. **Real file upload** (#2): stream every `IFormFile` to `{Root}/films/{FolderName}/{name}` and then create the `Photo`. Reject duplicates and non-image files. Remove `BufferBody` and stream instead (#13).
2. **Thumbnail service** (#3, #7, #17): move `Miniature()` out of the model into an `IThumbnailService`.
   - Generate thumbnails at upload time, in a background queue (`Channel<T>` + `BackgroundService`).
   - Key them by a content hash or `{Id}_{lastModified}` so they invalidate themselves.
   - Serve them with an endpoint `GET /thumb/{id}?size=s|m|l`, which generates on cache miss and returns a placeholder *without caching it*.
   - This also delivers the unfinished `Maximized()` (#9) as a `size=l` web preview, for example 2048 px.
3. Fix `AddToFilmByFile` to use `FilmId` (#1), sort lines in `Page` (#5), and assign `Place_In_Line` incrementally on upload (#6).
4. Add a `HomeController` with `Error` and a small dashboard, or remove the references (#8).
5. Make `Like` and every state change POST with an antiforgery token (#12). Use a small `fetch()` call so the page doesn't reload.
6. Deletes (#4): use **`Restrict`** for reference data (Format, Camera, FilmType, Developer, Recipe) so you can't delete them while they're in use. Keep cascade only for Film → Photo, and delete the files too, or move them to a `trash/` folder.

**Exit criterion:** you can upload a roll through the UI end to end, with no manual file copying, and see correct thumbnails.

## Phase 3: Finish v1 features (about 1 week)

These are the features the original design clearly aimed at:

- **Authentication:** a single owner login (ASP.NET Core Identity, or cookie auth with one configured user). Optionally a read-only "public gallery" mode (#11).
- **Album page editor:** drag and drop photos between lines and pages (SortableJS), which saves `Page`, `Line` and `Place_In_Line`. Add an "auto-layout film to a new page" action.
- **Lightbox viewer** on the Page and Film screens, with keyboard navigation and like or unlike.
- **Liked photos view** (the "Like" folder suggests this was planned).
- **Photo metadata:** restore `Date`, add `Notes` and frame number. Read EXIF from scans where present.
- **Search and filter:** films by camera, film stock, developer, date range and colour/BW. Photos by liked or film.
- **Paging** on `Photos/Index` (#18).

## Phase 4: Code quality (ongoing, in parallel with phases 2 and 3)

- Introduce **view models** in place of `ViewBag` (#20). Extract repeated queries into extension methods, for example `IQueryable<Film>.WithDetails()` (#19).
- Turn on `<TreatWarningsAsErrors>` for nullable warnings once they're cleaned up.
- Consistent naming: `IsLiked`, `PlaceInLine`, `DevTime` as a `TimeSpan`. This needs a migration.
- Tests: xUnit + `WebApplicationFactory` + an in-memory SQLite DB. Cover upload, thumbnails, page layout and delete rules.
- CI: a GitHub Actions workflow that builds, tests and builds the Docker image.

## Phase 5: Optional upgrades (pick what you want)

| Idea | Value |
|------|-------|
| **Docker / docker-compose** deployment to a NAS or home server | Run it anywhere, with the image volume mounted |
| **Stats dashboard**: rolls per year, favourite stock, developer usage, keeper rate (liked / total) | Fun for a film shooter, and cheap with the existing data |
| **Development timer**: start a recipe and get a countdown with agitation beeps | Uses the `Recipe` data directly |
| **Import from a folder**: point at a scan folder and create a Film and its Photos automatically | Removes the most tedious step |
| **Contact-sheet / album PDF export** of a page | Fits the "album page" concept |
| **REST API** (minimal APIs) plus a mobile-friendly UI | Log rolls from your phone while shooting |
| **UI refresh**: htmx or Blazor SSR in place of jQuery | Smaller JS, smoother interactions |
| **Cloud or object storage** (S3 / MinIO) behind `IFileStore` | Scales the archive beyond one disk |

---

## Suggested order and effort

```
Phase 0 ─► Phase 1 ─► Phase 2 ─► Phase 3 ─► Phase 5
 0.5 d      1–2 d      2–3 d      ~1 wk      as desired
                 └──── Phase 4 runs alongside ────┘
```

The minimum to call it **"finished" (v1.1)** is phases 0–2 plus authentication from phase 3.
