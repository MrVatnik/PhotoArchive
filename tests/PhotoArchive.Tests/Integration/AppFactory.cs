using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoArchive.Data;
using PhotoArchive.Models;

namespace PhotoArchive.Tests.Integration
{
    /// <summary>
    /// The whole app in-process: a SQLite file instead of SQL Server, and temp folders for originals and the cache.
    /// One factory (and so one database and archive) per test class.
    /// </summary>
    public class AppFactory : WebApplicationFactory<Program>
    {
        // a file, not ":memory:": requests and the background thumbnail queue each need their own connection
        private readonly string _dbDir = TestFiles.NewTempDir();
        private string ConnectionString => $"Data Source={Path.Combine(_dbDir, "photoarchive.db")}";

        public string OriginalsRoot { get; } = TestFiles.NewTempDir();
        public string CacheRoot { get; } = TestFiles.NewTempDir();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:OriginalsRoot"] = OriginalsRoot,
                ["Storage:ThumbnailsRoot"] = CacheRoot,
                ["Storage:WarmUpThumbnailsOnStartup"] = "false",
            }));
            builder.ConfigureServices(services =>
            {
                // replace the SQL Server registration (EF Core 9+ also registers an options-configuration delegate)
                foreach (var d in services.Where(d => d.ServiceType == typeof(DbContextOptions<PhotoContext>)
                                                   || d.ServiceType == typeof(IDbContextOptionsConfiguration<PhotoContext>)).ToList())
                    services.Remove(d);
                services.AddDbContext<PhotoContext>(o => o.UseSqlite(ConnectionString));
            });
        }

        public void Db(Action<PhotoContext> action)
        {
            using var scope = Services.CreateScope();
            action(scope.ServiceProvider.GetRequiredService<PhotoContext>());
        }

        public T Db<T>(Func<PhotoContext, T> query)
        {
            using var scope = Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<PhotoContext>());
        }

        /// <summary>A film with its reference data and the given files on disk (in a folder named like the film).</summary>
        public Film SeedFilm(string folder, string cameraName = "Canon F-1", string formatName = "35 мм", params string[] files)
        {
            return Db(db =>
            {
                var format = db.Formats.FirstOrDefault(f => f.Name == formatName) ?? db.Formats.Add(new Format { Name = formatName }).Entity;
                var camera = db.Cameras.FirstOrDefault(c => c.Name == cameraName) ?? db.Cameras.Add(new Camera { Name = cameraName, Format = format }).Entity;
                var stock = db.FilmTypes.FirstOrDefault(f => f.Name == "Agfa Aviphot") ?? db.FilmTypes.Add(new FilmType { Name = "Agfa Aviphot", ISO = 200 }).Entity;
                var dev = db.Developers.FirstOrDefault(d => d.Name == "Rodinal") ?? db.Developers.Add(new Developer { Name = "Rodinal" }).Entity;
                var recipe = db.Recipes.FirstOrDefault(r => r.FilmTypeId == stock.Id && r.DeveloperId == dev.Id)
                             ?? db.Recipes.Add(new Recipe { FilmType = stock, Developer = dev, EI = 200, Min = 8 }).Entity;
                var film = new Film { Camera = camera, Recipe = recipe, Date = new DateTime(2022, 1, 14), FolderName = folder };
                db.Films.Add(film);
                db.SaveChanges();

                foreach (var file in files)
                    TestFiles.WriteJpeg(Path.Combine(OriginalsRoot, folder, file));
                return film;
            });
        }

        public Photo SeedPhoto(int filmId, string pic, int page = 1, int line = 1, int place = 0) => Db(db =>
        {
            var photo = new Photo { FilmId = filmId, Name = pic, Pic = pic, Page = page, Line = line, Place_In_Line = place };
            db.Photos.Add(photo);
            db.SaveChanges();
            return photo;
        });

        public HttpClient NoRedirectClient() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        /// <summary>The antiforgery token rendered into a page's form.</summary>
        public static string Token(string html) =>
            Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                SqliteConnection.ClearAllPools();   // release the database file
                TestFiles.DeleteDir(_dbDir);
                TestFiles.DeleteDir(OriginalsRoot);
                TestFiles.DeleteDir(CacheRoot);
            }
        }
    }
}
