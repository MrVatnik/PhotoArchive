using System.Net;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace PhotoArchive.Tests.Integration
{
    public class MediaTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public MediaTests(AppFactory app) => _app = app;

        [Fact]
        public async Task Thumbnail_is_rendered_cached_and_revalidated()
        {
            var film = _app.SeedFilm("media-thumb", files: "a.jpg");
            var photo = _app.SeedPhoto(film.Id, "a.jpg");
            var client = _app.CreateClient();

            var first = await client.GetAsync($"/media/photo/{photo.Id}/thumb");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal("image/jpeg", first.Content.Headers.ContentType?.MediaType);
            using (var bmp = SKBitmap.Decode(await first.Content.ReadAsByteArrayAsync()))
                Assert.Equal(175, Math.Max(bmp.Width, bmp.Height));
            Assert.Single(Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{photo.Id}_*.jpg"));

            var etag = first.Headers.ETag!;
            var request = new HttpRequestMessage(HttpMethod.Get, $"/media/photo/{photo.Id}/thumb");
            request.Headers.IfNoneMatch.Add(etag);
            Assert.Equal(HttpStatusCode.NotModified, (await client.SendAsync(request)).StatusCode);
        }

        [Fact]
        public async Task Preview_is_capped_at_2048_and_never_upscaled()
        {
            var film = _app.SeedFilm("media-preview");
            TestFiles.WriteJpeg(Path.Combine(_app.OriginalsRoot, "media-preview", "big.jpg"), 3000, 2000);
            TestFiles.WriteJpeg(Path.Combine(_app.OriginalsRoot, "media-preview", "small.jpg"), 800, 600);
            var big = _app.SeedPhoto(film.Id, "big.jpg");
            var small = _app.SeedPhoto(film.Id, "small.jpg");
            var client = _app.CreateClient();

            using (var bmp = SKBitmap.Decode(await client.GetByteArrayAsync($"/media/photo/{big.Id}/preview")))
                Assert.Equal((2048, 1365), (bmp.Width, bmp.Height));
            using (var bmp = SKBitmap.Decode(await client.GetByteArrayAsync($"/media/photo/{small.Id}/preview")))
                Assert.Equal((800, 600), (bmp.Width, bmp.Height));
        }

        [Fact]
        public async Task Missing_original_gives_placeholder_that_is_not_cached()
        {
            var film = _app.SeedFilm("media-missing");
            var photo = _app.SeedPhoto(film.Id, "not-there.jpg");

            var response = await _app.CreateClient().GetAsync($"/media/photo/{photo.Id}/thumb");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
            Assert.Empty(Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{photo.Id}_*.jpg"));
        }

        [Fact]
        public async Task Changed_original_gets_a_fresh_thumbnail()
        {
            var film = _app.SeedFilm("media-change", files: "c.jpg");
            var photo = _app.SeedPhoto(film.Id, "c.jpg");
            var client = _app.CreateClient();
            await client.GetAsync($"/media/photo/{photo.Id}/thumb");
            var before = Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{photo.Id}_*.jpg").Single();

            TestFiles.WriteJpeg(Path.Combine(_app.OriginalsRoot, "media-change", "c.jpg"), 640, 480);
            File.SetLastWriteTimeUtc(Path.Combine(_app.OriginalsRoot, "media-change", "c.jpg"), DateTime.UtcNow.AddMinutes(1));
            await client.GetAsync($"/media/photo/{photo.Id}/thumb");

            var after = Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{photo.Id}_*.jpg").Single();
            Assert.NotEqual(before, after);   // new stamp, stale file removed
        }

        [Fact]
        public async Task Originals_in_nested_folders_are_served()
        {
            var film = _app.SeedFilm("roll-nested/Export", files: "n 1.jpg");
            var photo = _app.SeedPhoto(film.Id, "n 1.jpg");
            var client = _app.CreateClient();

            var html = await client.GetStringAsync($"/Photos/Details/{photo.Id}");
            Assert.Contains("/media/originals/roll-nested/Export/n%201.jpg", html);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/media/originals/roll-nested/Export/n%201.jpg")).StatusCode);
        }

        [Theory]
        [InlineData("/media/photo/999999/thumb", HttpStatusCode.NotFound)]
        [InlineData("/media/photo/1/bogus", HttpStatusCode.NotFound)]
        [InlineData("/Home/Error", HttpStatusCode.OK)]
        public async Task Routes(string url, HttpStatusCode expected)
        {
            Assert.Equal(expected, (await _app.CreateClient().GetAsync(url)).StatusCode);
        }

        [Fact]
        public async Task Home_redirects_to_films()
        {
            var response = await _app.NoRedirectClient().GetAsync("/Home");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location?.OriginalString);   // Films/Index is the default route
            Assert.Equal(HttpStatusCode.OK, (await _app.CreateClient().GetAsync("/")).StatusCode);
        }

        [Fact]
        public void Model_and_migrations_are_in_sync()
        {
            // the real app runs on SQL Server migrations: a model change without a migration must fail the build
            var options = new DbContextOptionsBuilder<PhotoArchive.Data.PhotoContext>()
                .UseSqlServer("Server=(localdb)\\none;Database=none").Options;
            using var db = new PhotoArchive.Data.PhotoContext(options);
            Assert.False(db.Database.HasPendingModelChanges(), "Run `dotnet ef migrations add <Name>` for the model change.");
        }
    }
}
