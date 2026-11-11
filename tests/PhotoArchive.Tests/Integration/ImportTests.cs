using System.Net;
using System.Text.RegularExpressions;

namespace PhotoArchive.Tests.Integration
{
    /// <summary>The archive importer end to end, on a small fake archive shaped like the real one.</summary>
    public class ImportTests : IDisposable
    {
        private readonly AppFactory _app = new();

        public ImportTests()
        {
            var root = _app.OriginalsRoot;
            void Roll(string folder, string? txt, int images, string? sub = null)
            {
                var dir = Path.Combine(root, folder);
                Directory.CreateDirectory(dir);
                if (txt != null)
                    File.WriteAllText(Path.Combine(dir, "Пленка+Аппарат.txt"), txt);
                for (var i = 1; i <= images; i++)
                    TestFiles.WriteJpeg(Path.Combine(dir, sub ?? "", $"{i:000}.jpg"), 60, 40);
            }

            Roll("23-03-23_bw_Zorki1", "aviphot as 400 + PYRO350 + Зоркий 1 + Юпитер 12 \r\nстр 61", 8);
            Roll("23-09-10_bw_Zeiss Ikon Maximar", "kodak aerocolor +  с41 + Zeiss Ikon Maximar 207/1\nстр 79", 3);
            Roll("23-09-10_col_MinoltaflexII", "kodak aerocolor +  с41 + Minoltaflex II\nстр 79", 6);
            // TIFF scans at the top, JPEG export in a same-named sub-folder, editor sidecars next to it
            Roll("22-04-04_bw_F-1", "ORWO DN1 0.25 + canon f-1", 4, sub: "22-04-04_bw_F-1");
            File.WriteAllBytes(Path.Combine(root, "22-04-04_bw_F-1", "000001.tif"), new byte[] { 1, 2, 3 });
            Roll("22-04-04_bw_F-1/CaptureOne", null, 9);
            Roll("22-07-29_bw_LCA", "кн2 как 25 + AF pyro350 + ЛК-А", 2);
            Roll("23-06-25_BW_MinoltaflexII-RESCAN", "kodak tmax 100 +  ФГ31П  + Minoltaflex II\r\nстр 68", 2);
            Roll("23-07-13_col_Vera", null, 2);
            Roll("папа маленький", null, 1);
            Roll("21-05-30_bw_FED2", "Foma pan 400 + ФЭД-2", 1);
            _app.SeedFilm("21-05-30_bw_FED2");   // already registered

            TestFiles.WriteAlbumDocx(Path.Combine(root, "Плёнки описание.docx"),
                new[] { "Адрес", "", "Плёнка", "Аппарат", "Дата ~" },
                new[] { "27 – 1", "27 – 7", "ORWO DN1 0.25", "Canon F-1", "04.04.22" });
        }

        public void Dispose() => _app.Dispose();

        private record Row(string Folder, bool Checked, string Html);

        private static List<Row> Rows(string html) =>
            Regex.Matches(html, @"<tr class=""[^""]*"">(.*?)</tr>", RegexOptions.Singleline)
                .Select(m => m.Groups[1].Value)
                .Select(r => new Row(
                    WebUtility.HtmlDecode(Regex.Match(r, "<strong>(.*?)</strong>").Groups[1].Value),
                    Regex.IsMatch(r, @"row-include""\s*checked"),
                    r))
                .ToList();

        private static string Field(Row row, string name) =>
            WebUtility.HtmlDecode(Regex.Match(row.Html, $@"name=""rows\[\d+\]\.{name}"" value=""([^""]*)""").Groups[1].Value);

        /// <summary>What a browser would send for the form: every field, but only checked, enabled checkboxes.</summary>
        private static List<KeyValuePair<string, string>> BrowserForm(string html)
        {
            var form = Regex.Match(html, @"<form[^>]*action=""/Import""[^>]*>(.*?)</form>", RegexOptions.Singleline).Groups[1].Value;
            var fields = new List<KeyValuePair<string, string>>();
            foreach (Match input in Regex.Matches(form, "<input[^>]*>"))
            {
                var tag = input.Value;
                var name = Regex.Match(tag, "name=\"([^\"]+)\"");
                if (!name.Success)
                    continue;
                if (tag.Contains("type=\"checkbox\"") && (!Regex.IsMatch(tag, @"\schecked") || tag.Contains(" disabled")))
                    continue;
                var value = Regex.Match(tag, "value=\"([^\"]*)\"");
                fields.Add(new(WebUtility.HtmlDecode(name.Groups[1].Value), WebUtility.HtmlDecode(value.Groups[1].Value)));
            }
            return fields;
        }

        [Fact]
        public async Task Scan_prefills_and_preselects_rows()
        {
            var html = await _app.CreateClient().GetStringAsync("/Import");
            var rows = Rows(html);

            Assert.Equal(new[]
            {
                "22-04-04_bw_F-1", "22-07-29_bw_LCA", "23-03-23_bw_Zorki1", "23-06-25_BW_MinoltaflexII-RESCAN",
                "23-07-13_col_Vera", "23-09-10_bw_Zeiss Ikon Maximar", "23-09-10_col_MinoltaflexII"
            }, rows.Select(r => r.Folder));
            Assert.Contains("папа маленький", WebUtility.HtmlDecode(html));   // listed as skipped

            var selected = rows.Where(r => r.Checked).Select(r => r.Folder).ToList();
            Assert.DoesNotContain("23-06-25_BW_MinoltaflexII-RESCAN", selected);
            Assert.DoesNotContain("23-07-13_col_Vera", selected);
            Assert.Equal(5, selected.Count);

            var orwo = rows.Single(r => r.Folder == "22-04-04_bw_F-1");
            Assert.Equal("22-04-04_bw_F-1/22-04-04_bw_F-1", Field(orwo, "ImageFolder"));   // not CaptureOne
            Assert.Equal(("ORWO DN1", "0.25", "27", "1"), (Field(orwo, "Stock"), Field(orwo, "Iso"), Field(orwo, "Page"), Field(orwo, "Line")));

            var minolta = rows.Single(r => r.Folder == "23-09-10_col_MinoltaflexII");
            Assert.Equal(("79", "3"), (Field(minolta, "Page"), Field(minolta, "Line")));   // after the 6x9 roll's 2 strips
        }

        [Fact]
        public async Task Import_creates_films_photos_and_reference_data_once()
        {
            var client = _app.CreateClient();
            var form = BrowserForm(await client.GetStringAsync("/Import"));

            var result = await client.PostAsync("/Import", new FormUrlEncodedContent(form));
            var page = WebUtility.HtmlDecode(await result.Content.ReadAsStringAsync());
            Assert.Contains("Imported 5 film(s) with 23 photo(s).", page);

            // layout: frames per album strip follow the format
            string Layout(string folder) => string.Join(" ", _app.Db(db => db.Photos
                .Where(p => p.Film!.FolderName.StartsWith(folder))
                .GroupBy(p => new { p.Page, p.Line }).OrderBy(g => g.Key.Page).ThenBy(g => g.Key.Line)
                .Select(g => $"{g.Key.Page}-{g.Key.Line}:{g.Count()}").ToList()));
            Assert.Equal("61-1:6 61-2:2", Layout("23-03-23_bw_Zorki1"));              // 35 mm: 6 per strip
            Assert.Equal("79-1:2 79-2:1", Layout("23-09-10_bw_Zeiss"));               // 6x9: 2 per strip
            Assert.Equal("79-3:3 79-4:3", Layout("23-09-10_col_MinoltaflexII"));      // 6x6: 3 per strip, after the Zeiss roll
            Assert.Equal("27-1:4", Layout("22-04-04_bw_F-1"));                        // from the album index
            Assert.Equal("0-0:2", Layout("22-07-29_bw_LCA"));                         // not in the album

            var films = _app.Db(db => db.Films.Select(f => new
            {
                f.FolderName, Camera = f.Camera!.Name, Format = f.Camera.Format!.Name,
                Stock = f.Recipe!.FilmType!.Name, f.Recipe.FilmType.ISO, f.Recipe.EI, Dev = f.Recipe.Developer!.Name
            }).ToList());
            var orwo = films.Single(f => f.FolderName == "22-04-04_bw_F-1/22-04-04_bw_F-1");
            Assert.Equal(("ORWO DN1", 0.25m, 0.25m, "BW in lab"), (orwo.Stock, orwo.ISO, orwo.EI, orwo.Dev));
            var kn2 = films.Single(f => f.FolderName == "22-07-29_bw_LCA");
            Assert.Equal(("ЛК-А", "КН-2", 25m, 25m, "AF Pyro-350"), (kn2.Camera, kn2.Stock, kn2.ISO, kn2.EI, kn2.Dev));
            var zeiss = films.Single(f => f.FolderName == "23-09-10_bw_Zeiss Ikon Maximar");
            Assert.Equal(("Zeiss Ikon Maximar 207/1 (6x9)", "Средний 6x9", "С-41"), (zeiss.Camera, zeiss.Format, zeiss.Dev));
            var minolta = films.Single(f => f.FolderName == "23-09-10_col_MinoltaflexII");
            Assert.Equal("Средний 6x6", minolta.Format);
            var zorki = films.Single(f => f.FolderName == "23-03-23_bw_Zorki1");
            Assert.Equal(("Зоркий 1", "Agfa Aviphot", 400m, "AF Pyro-350"), (zorki.Camera, zorki.Stock, zorki.EI, zorki.Dev));

            // the two Aerocolor rolls (6x9 and 6x6) share one film type and one recipe; ЛК-А reused its camera only once
            Assert.Equal(1, _app.Db(db => db.FilmTypes.Count(f => f.Name == "Kodak Aerocolor")));
            Assert.Equal(1, _app.Db(db => db.Cameras.Count(c => c.Name == "Minoltaflex II")));

            // submitting the same form again adds nothing
            var filmCount = _app.Db(db => db.Films.Count());
            var again = WebUtility.HtmlDecode(await (await client.PostAsync("/Import", new FormUrlEncodedContent(form))).Content.ReadAsStringAsync());
            Assert.Equal(filmCount, _app.Db(db => db.Films.Count()));
            Assert.Contains("Skipped 5 already imported.", again);

            // only the deliberately unchecked rows are left
            Assert.Equal(new[] { "23-06-25_BW_MinoltaflexII-RESCAN", "23-07-13_col_Vera" },
                Rows(await client.GetStringAsync("/Import")).Select(r => r.Folder));
        }

        [Fact]
        public async Task Rows_with_missing_data_are_reported_not_imported()
        {
            var client = _app.CreateClient();
            var html = await client.GetStringAsync("/Import");
            var form = BrowserForm(html);
            var vera = Rows(html).FindIndex(r => r.Folder == "23-07-13_col_Vera");
            // user ticks Vera without filling in a stock: like a browser, the checkbox's "true" comes before its hidden "false"
            var key = $"rows[{vera}].Include";
            form.Insert(form.FindIndex(f => f.Key == key), new(key, "true"));

            var page = WebUtility.HtmlDecode(await (await client.PostAsync("/Import", new FormUrlEncodedContent(form))).Content.ReadAsStringAsync());

            Assert.Contains("23-07-13_col_Vera: film stock is required", page);
            Assert.False(_app.Db(db => db.Films.Any(f => f.FolderName.StartsWith("23-07-13_col_Vera"))));
        }
    }
}
