using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using PhotoArchive.Models;

namespace PhotoArchive.Tests.Integration
{
    /// <summary>Upload, Like and delete rules.</summary>
    public class PhotoActionsTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public PhotoActionsTests(AppFactory app) => _app = app;

        private static MultipartFormDataContent UploadForm(int filmId, int page, int line, string? token, params (string Name, byte[] Bytes)[] files)
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(filmId.ToString()), "FilmId" },
                { new StringContent(page.ToString()), "Page" },
                { new StringContent(line.ToString()), "Line" }
            };
            if (token != null)
                form.Add(new StringContent(token), "__RequestVerificationToken");
            foreach (var (name, bytes) in files)
            {
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(content, "uploadedFiles", name);
            }
            return form;
        }

        [Fact]
        public async Task Upload_saves_valid_images_and_skips_the_rest()
        {
            var film = _app.SeedFilm("upload-roll", files: "existing.jpg");
            _app.SeedPhoto(film.Id, "existing.jpg", page: 5, line: 2, place: 0);
            var originalBytes = File.ReadAllBytes(Path.Combine(_app.OriginalsRoot, "upload-roll", "existing.jpg"));
            var client = _app.NoRedirectClient();

            var page = await client.GetStringAsync($"/Photos/AddToFilmByFile/{film.Id}");
            // suggestion: the film's last page, next free strip
            Assert.Matches("id=\"Page\"[^>]*value=\"5\"|value=\"5\"[^>]*id=\"Page\"", page);
            Assert.Matches("id=\"Line\"[^>]*value=\"3\"|value=\"3\"[^>]*id=\"Line\"", page);

            var response = await client.PostAsync("/Photos/AddToFilmByFiles", UploadForm(film.Id, 5, 2, AppFactory.Token(page),
                ("b.jpg", TestFiles.Jpeg()),
                ("a.JPG", TestFiles.Jpeg()),
                ("existing.jpg", TestFiles.Jpeg(50, 50)),      // already in the film
                ("fake.jpg", "not an image"u8.ToArray()),     // not decodable
                ("notes.txt", "hello"u8.ToArray())));          // wrong type

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var added = _app.Db(db => db.Photos.Where(p => p.FilmId == film.Id && p.Pic != "existing.jpg")
                .OrderBy(p => p.Place_In_Line).Select(p => new { p.Pic, p.Page, p.Line, p.Place_In_Line }).ToList());
            // sorted by name, continuing after the photo already in strip 5-2
            Assert.Equal(new[] { ("a.JPG", 1), ("b.jpg", 2) }, added.Select(p => (p.Pic!, p.Place_In_Line)));
            Assert.All(added, p => Assert.Equal((5, 2), (p.Page, p.Line)));

            var folder = Path.Combine(_app.OriginalsRoot, "upload-roll");
            Assert.True(File.Exists(Path.Combine(folder, "a.JPG")));
            Assert.False(File.Exists(Path.Combine(folder, "fake.jpg")));
            Assert.False(File.Exists(Path.Combine(folder, "notes.txt")));
            Assert.Empty(Directory.GetFiles(folder, "*.uploading"));
            Assert.Equal(originalBytes, File.ReadAllBytes(Path.Combine(folder, "existing.jpg")));   // never overwritten

            var details = await client.GetStringAsync($"/Films/Details/{film.Id}");
            Assert.Contains("Added 2 photo(s). Skipped 3", WebUtility.HtmlDecode(details));
        }

        [Fact]
        public async Task Upload_without_antiforgery_token_is_rejected()
        {
            var film = _app.SeedFilm("upload-csrf");
            var response = await _app.NoRedirectClient().PostAsync("/Photos/AddToFilmByFiles", UploadForm(film.Id, 1, 1, null, ("x.jpg", TestFiles.Jpeg())));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, _app.Db(db => db.Photos.Count(p => p.FilmId == film.Id)));
        }

        [Fact]
        public async Task Like_only_changes_state_on_post()
        {
            var film = _app.SeedFilm("like-roll", files: "l.jpg");
            var photo = _app.SeedPhoto(film.Id, "l.jpg");
            var client = _app.NoRedirectClient();

            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync($"/Photos/Like/{photo.Id}")).StatusCode);
            Assert.False(_app.Db(db => db.Photos.Find(photo.Id)!.Is_Liked));

            var details = await client.GetStringAsync($"/Photos/Details/{photo.Id}");
            var response = await client.PostAsync($"/Photos/Like/{photo.Id}",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = AppFactory.Token(details) }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.True(_app.Db(db => db.Photos.Find(photo.Id)!.Is_Liked));
        }

        [Fact]
        public async Task Reference_data_in_use_cannot_be_deleted()
        {
            _app.SeedFilm("delete-guard", cameraName: "Guarded Camera", formatName: "Guarded Format");
            var formatId = _app.Db(db => db.Formats.Single(f => f.Name == "Guarded Format").Id);
            var client = _app.NoRedirectClient();

            var page = await client.GetStringAsync($"/Formats/Delete/{formatId}");
            Assert.DoesNotContain("value=\"Delete\"", page);
            Assert.Contains("still in use", page);

            var response = await client.PostAsync($"/Formats/Delete/{formatId}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = formatId.ToString(),
                ["__RequestVerificationToken"] = AppFactory.Token(page)
            }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.True(_app.Db(db => db.Formats.Any(f => f.Id == formatId)));
            var after = await client.GetStringAsync(response.Headers.Location!.OriginalString);
            Assert.Contains("still used by cameras", after);
        }

        [Fact]
        public async Task Unused_reference_data_can_be_deleted()
        {
            var id = _app.Db(db => { var f = new Format { Name = "Unused Format" }; db.Formats.Add(f); db.SaveChanges(); return f.Id; });
            var client = _app.NoRedirectClient();
            var page = await client.GetStringAsync($"/Formats/Delete/{id}");

            await client.PostAsync($"/Formats/Delete/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["__RequestVerificationToken"] = AppFactory.Token(page)
            }));
            Assert.False(_app.Db(db => db.Formats.Any(f => f.Id == id)));
        }

        [Fact]
        public async Task Deleting_a_film_removes_records_and_cache_but_keeps_originals()
        {
            var film = _app.SeedFilm("delete-film", files: new[] { "d1.jpg", "d2.jpg" });
            var p1 = _app.SeedPhoto(film.Id, "d1.jpg");
            _app.SeedPhoto(film.Id, "d2.jpg");
            var client = _app.NoRedirectClient();
            await client.GetAsync($"/media/photo/{p1.Id}/thumb");   // put something in the cache
            Assert.NotEmpty(Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{p1.Id}_*.jpg"));

            var page = await client.GetStringAsync($"/Films/Delete/{film.Id}");
            await client.PostAsync($"/Films/Delete/{film.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = film.Id.ToString(),
                ["__RequestVerificationToken"] = AppFactory.Token(page)
            }));

            Assert.False(_app.Db(db => db.Films.Any(f => f.Id == film.Id)));
            Assert.Equal(0, _app.Db(db => db.Photos.Count(p => p.FilmId == film.Id)));
            Assert.Equal(2, Directory.GetFiles(Path.Combine(_app.OriginalsRoot, "delete-film")).Length);
            Assert.Empty(Directory.GetFiles(Path.Combine(_app.CacheRoot, "thumb"), $"{p1.Id}_*.jpg"));
        }

        [Fact]
        public async Task Album_page_lines_are_sorted()
        {
            var film = _app.SeedFilm("page-order", files: new[] { "x.jpg", "y.jpg", "z.jpg" });
            var z = _app.SeedPhoto(film.Id, "z.jpg", page: 900, line: 3);
            var x = _app.SeedPhoto(film.Id, "x.jpg", page: 900, line: 1);
            var y = _app.SeedPhoto(film.Id, "y.jpg", page: 900, line: 2);

            var html = await _app.CreateClient().GetStringAsync("/Photos/Page?Num=900");
            var ids = Regex.Matches(html, @"/media/photo/(\d+)/thumb").Select(m => int.Parse(m.Groups[1].Value)).ToList();
            Assert.Equal(new[] { x.Id, y.Id, z.Id }, ids);
        }
    }
}
