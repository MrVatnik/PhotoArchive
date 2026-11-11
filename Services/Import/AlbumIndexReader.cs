using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace PhotoArchive.Services.Import
{
    /// <summary>One roll from the paper album's index: where its negatives are filed.</summary>
    public record AlbumRoll(int StartPage, int StartLine, int EndPage, int EndLine, string Stock, string Camera, IReadOnlyList<DateTime> Dates)
    {
        public string Address => $"{StartPage}-{StartLine}..{EndPage}-{EndLine}";
    }

    /// <summary>
    /// Reads the roll index (first table) of the album description .docx ("Плёнки описание.docx"):
    /// columns are start address, end address, film, camera, date. Frame captions in the other tables are not read.
    /// </summary>
    public static class AlbumIndexReader
    {
        private static readonly Regex Address = new(@"^\s*(\d+)\s*[–-]\s*(\d+)\s*$");
        private static readonly Regex DateRx = new(@"(\d{1,2})\.(\d{1,2})(?:\.(\d{2,4}))?");

        public static IReadOnlyList<AlbumRoll> Read(string docxPath)
        {
            using var zip = ZipFile.OpenRead(docxPath);
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null)
                return Array.Empty<AlbumRoll>();
            string xml;
            using (var reader = new StreamReader(entry.Open()))
                xml = reader.ReadToEnd();

            var table = Regex.Match(xml, @"<w:tbl>.*?</w:tbl>", RegexOptions.Singleline);
            if (!table.Success)
                return Array.Empty<AlbumRoll>();

            var rolls = new List<AlbumRoll>();
            foreach (Match row in Regex.Matches(table.Value, @"<w:tr[ >].*?</w:tr>", RegexOptions.Singleline))
            {
                var cells = Regex.Matches(row.Value, @"<w:tc>.*?</w:tc>", RegexOptions.Singleline).Select(c => CellText(c.Value)).ToList();
                if (cells.Count < 5)
                    continue;
                var a = Address.Match(cells[0]);
                var b = Address.Match(cells[1]);
                if (!a.Success || !b.Success || string.IsNullOrWhiteSpace(cells[2]))
                    continue;

                rolls.Add(new AlbumRoll(
                    int.Parse(a.Groups[1].Value), int.Parse(a.Groups[2].Value),
                    int.Parse(b.Groups[1].Value), int.Parse(b.Groups[2].Value),
                    cells[2], cells[3], ParseDates(cells[4])));
            }
            return rolls;
        }

        /// <summary>"14.10.21 (30.11)" → 2021-10-14 and 2021-11-30 (the second date borrows the first one's year).</summary>
        private static List<DateTime> ParseDates(string text)
        {
            var dates = new List<DateTime>();
            int? year = null;
            foreach (Match m in DateRx.Matches(text))
            {
                if (m.Groups[3].Success)
                    year = int.Parse(m.Groups[3].Value) is var y && y < 100 ? 2000 + y : y;
                if (year == null)
                    continue;
                if (DateTime.TryParseExact($"{year:0000}-{int.Parse(m.Groups[2].Value):00}-{int.Parse(m.Groups[1].Value):00}",
                        "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    dates.Add(d);
            }
            return dates;
        }

        private static string CellText(string cellXml)
        {
            var paragraphs = Regex.Matches(cellXml, @"<w:p[ >].*?</w:p>", RegexOptions.Singleline)
                .Select(p => string.Concat(Regex.Matches(p.Value, @"<w:t[^>]*>([^<]*)</w:t>").Select(t => t.Groups[1].Value)));
            return WebUtility.HtmlDecode(string.Join(" ", paragraphs)).Trim();
        }
    }
}
