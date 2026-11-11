using System.Globalization;
using System.Text.RegularExpressions;

namespace PhotoArchive.Services.Import
{
    /// <summary>What could be read about a roll from its folder name and its "Пленка+Аппарат.txt".</summary>
    public class RollMetadata
    {
        public DateTime? Date { get; set; }
        public bool? Color { get; set; }
        public bool IsRescan { get; set; }

        public string? Stock { get; set; }
        public decimal? Iso { get; set; }
        public decimal? Ei { get; set; }
        public bool? StockColor { get; set; }
        public string? Developer { get; set; }
        public string? Camera { get; set; }
        public string? CameraFormat { get; set; }
        public int? AlbumPage { get; set; }

        public string? RawText { get; set; }
        public List<string> Notes { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    public static class RollMetadataParser
    {
        private static readonly Regex FolderRx = new(@"^(\d{2})-(\d{2})-(\d{2})_(col|bw)_(.+?)(-RESCAN)?$", RegexOptions.IgnoreCase);
        private static readonly Regex PlusRx = new(@"(?<=^|\s)\+(?=\s|$)");                 // " + " but not "HP5+"
        private static readonly Regex EiRx = new(@"\s+(?:as|как)\s+(\d+(?:[.,]\d+)?)(?:\s*-\s*(\d+(?:[.,]\d+)?))?(?:\s*EI)?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex TrailingIsoRx = new(@"^(.*\S)\s+(\d+(?:[.,]\d+)?)\s*([dt])?$", RegexOptions.IgnoreCase);
        private static readonly Regex LeadingIsoRx = new(@"^(\d+(?:[.,]\d+)?)([dt])?", RegexOptions.IgnoreCase);
        private static readonly Regex PageRx = new(@"^\s*стр\.?\s*-?\s*(\d+)", RegexOptions.IgnoreCase);

        /// <summary>True if the folder name looks like a roll ("yy-MM-dd_col|bw_Camera").</summary>
        public static bool IsRollFolder(string folderName) => FolderRx.IsMatch(folderName);

        public static RollMetadata Parse(string folderName, string? txt)
        {
            var meta = new RollMetadata { RawText = txt?.Trim() };

            var m = FolderRx.Match(folderName);
            string? folderCamera = null;
            if (m.Success)
            {
                if (DateTime.TryParseExact($"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}", "yy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    meta.Date = date;
                meta.Color = m.Groups[4].Value.Equals("col", StringComparison.OrdinalIgnoreCase);
                folderCamera = m.Groups[5].Value.Replace('_', ' ');
                meta.IsRescan = m.Groups[6].Success;
            }

            if (string.IsNullOrWhiteSpace(txt))
            {
                meta.Warnings.Add("No metadata .txt");
            }
            else
            {
                var lines = txt.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                ParseMainLine(lines[0], meta);
                foreach (var line in lines.Skip(1))
                {
                    var p = PageRx.Match(line);
                    if (p.Success && meta.AlbumPage == null)
                        meta.AlbumPage = int.Parse(p.Groups[1].Value);
                    else
                        meta.Notes.Add(line);
                }
            }

            // camera: from the .txt, otherwise from the folder name
            if (meta.Camera == null && folderCamera != null)
            {
                var cam = ImportCatalog.FindCamera(folderCamera);
                meta.Camera = cam?.Name ?? folderCamera;
                meta.CameraFormat = cam?.Format;
            }

            // developer: lab default when the notes don't name one
            var colorRoll = meta.Color ?? meta.StockColor ?? false;
            if (meta.Developer == null)
            {
                var stock = ImportCatalog.Stocks.FirstOrDefault(s => s.Name == meta.Stock);
                meta.Developer = colorRoll ? stock?.DefaultColorDeveloper ?? ImportCatalog.LabC41 : ImportCatalog.LabBw;
            }

            // EI: from the notes, otherwise box speed; cameras with a limited dial snap to their nearest setting
            meta.Ei ??= meta.Iso;
            if (meta.Ei is decimal ei && meta.Camera != null && ImportCatalog.EiSettings.TryGetValue(meta.Camera, out var settings)
                && !settings.Contains(ei))
            {
                var nearest = ImportCatalog.NearestSetting(settings, ei);
                meta.Notes.Add($"{meta.Camera} has no EI {ei:0.##} setting: using {nearest:0.##}");
                meta.Ei = nearest;
            }
            if (meta.Iso == null)
                meta.Warnings.Add("Film speed (ISO) unknown");

            return meta;
        }

        private static void ParseMainLine(string line, RollMetadata meta)
        {
            // pull out "(...)" notes, including nested brackets
            line = ExtractBrackets(line, meta.Notes);

            var parts = PlusRx.Split(line).Select(p => p.Trim().Trim('+').Trim()).Where(p => p.Length > 0).ToList();
            if (parts.Count == 0)
                return;

            // the camera is whichever part matches a known camera (it is not always last: lenses follow on Zorki rolls)
            var camIndex = parts.FindIndex(1, p => ImportCatalog.FindCamera(p) != null);
            if (camIndex < 0 && parts.Count > 1)
                camIndex = parts.Count - 1; // unknown camera: assume the last part
            if (camIndex > 0)
            {
                var cam = ImportCatalog.FindCamera(parts[camIndex]);
                meta.Camera = cam?.Name ?? parts[camIndex];
                meta.CameraFormat = cam?.Format;
                foreach (var extra in parts.Skip(camIndex + 1))
                    meta.Notes.Add("Lens/extra: " + extra);
            }

            var middle = camIndex > 0 ? parts.Skip(1).Take(camIndex - 1).ToList() : parts.Skip(1).ToList();
            if (middle.Count > 0)
            {
                meta.Developer = ImportCatalog.FindDeveloper(middle[0]) ?? middle[0];
                foreach (var extra in middle.Skip(1))
                    meta.Notes.Add(extra);
            }

            ParseStock(parts[0], meta);
        }

        /// <summary>Parses just a film-stock description ("Kodak Ektar 100 (120)", "Aviphot как 90").</summary>
        public static RollMetadata ParseStockText(string text)
        {
            var meta = new RollMetadata();
            ParseStock(ExtractBrackets(text, meta.Notes), meta);
            return meta;
        }

        private static void ParseStock(string text, RollMetadata meta)
        {
            var ei = EiRx.Match(text);
            if (ei.Success)
            {
                meta.Ei = ParseNumber(ei.Groups[1].Value);
                if (ei.Groups[2].Success)
                    meta.Notes.Add($"EI range {ei.Groups[1].Value}-{ei.Groups[2].Value}");
                text = text[..ei.Index];
            }

            var (stock, rest) = ImportCatalog.MatchPrefix(text, ImportCatalog.Stocks, s => s.Aliases.Append(s.Name));
            if (stock != null)
            {
                meta.Stock = stock.Name;
                meta.StockColor = stock.Color;
                var iso = LeadingIsoRx.Match(rest);
                if (iso.Success && iso.Length > 0)
                {
                    meta.Iso = ParseNumber(iso.Groups[1].Value);
                    // Vision3 comes as daylight (d) or tungsten (t)
                    if (stock.Name == "Kodak Vision3 d" && iso.Groups[2].Value.Equals("t", StringComparison.OrdinalIgnoreCase))
                        meta.Stock = "Kodak Vision3 t";
                }
                else
                {
                    meta.Iso = stock.Iso;
                }
                return;
            }

            // unknown stock: "<name> <iso>[d|t]" or just a name
            var t = TrailingIsoRx.Match(text.Trim());
            if (t.Success)
            {
                meta.Stock = t.Groups[1].Value.Trim();
                meta.Iso = ParseNumber(t.Groups[2].Value);
            }
            else
            {
                meta.Stock = text.Trim();
            }
            meta.Warnings.Add($"Unknown film stock \"{meta.Stock}\"");
        }

        private static string ExtractBrackets(string text, List<string> notes)
        {
            var result = new System.Text.StringBuilder();
            int depth = 0, start = -1;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '(')
                {
                    if (depth++ == 0) start = i;
                }
                else if (text[i] == ')' && depth > 0)
                {
                    if (--depth == 0) notes.Add(text[(start + 1)..i].Trim());
                }
                else if (depth == 0)
                {
                    result.Append(text[i]);
                }
            }
            return Regex.Replace(result.ToString(), @"\s{2,}", " ").Trim();
        }

        private static decimal ParseNumber(string s) => decimal.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}
