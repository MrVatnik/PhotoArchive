using System.Text;

namespace PhotoArchive.Services.Import
{
    /// <summary>A known film stock and how it is spelled in the archive's notes.</summary>
    public record StockInfo(string Name, decimal? Iso, bool? Color, string? DefaultColorDeveloper, params string[] Aliases);

    /// <summary>A known camera and how it is spelled in folder names and notes.</summary>
    public record CameraInfo(string Name, string Format, params string[] Aliases);

    /// <summary>
    /// Spelling variants found in the archive (see docs/ARCHIVE_SOURCES.md), mapped to the names used in the database.
    /// Matching uses <see cref="Key"/>, which ignores case, spaces and punctuation and folds Cyrillic/Latin look-alikes.
    /// </summary>
    public static class ImportCatalog
    {
        public const string LabC41 = "С-41";       // Cyrillic С, as in the existing database
        public const string LabBw = "BW in lab";
        public const string Ecn2 = "ECN-2";

        // Longer aliases first is not required: the matcher always picks the longest matching alias.
        public static readonly StockInfo[] Stocks =
        {
            new("Fujifilm C", 200, true, null, "Fujifilm C", "fujicolor c", "fuji c"),
            new("Foma pan", null, false, null, "Foma pan", "Fomapan", "Foma"),
            new("Kodak gold", 200, true, null, "Kodak gold"),
            new("Kodak Ektar", 100, true, null, "Kodak Ektar"),
            new("Kodak UltraMax", 400, true, null, "Kodak UltraMax"),
            new("Fuji superia", 200, true, null, "Fuji superia", "Fujifilm superia"),
            new("Тасма Т25", null, false, null, "Тасма Т25", "Тасма т25", "tasma t25"),
            new("Kodak Aerocolor", 125, true, null, "Kodak Aerocolor", "kodak aerocoloar"),
            new("Silberra ultima", 100, false, null, "Silberra ultima"),
            new("Silberra color", null, true, null, "Silberra color"),
            new("Kodak ColorPlus", 200, true, null, "Kodak ColorPlus"),
            // Rollei Superpan 200 is the same emulsion as Agfa Aviphot 200 (confirmed by the owner)
            new("Agfa Aviphot", 200, false, null, "Agfa Aviphot", "Aviphot", "Rollei Superpan", "Rollei superpan"),
            new("Kodak Vision3 d", 250, true, Ecn2, "Kodak Vision3", "Kodak Vision 3", "Kodak visionn 3"),
            new("Kodak Vision3 t", 500, true, Ecn2),   // reached from "Vision3 500t" via the d/t suffix
            new("Fujifilm Eterna", null, true, Ecn2, "Fujifilm Eterna", "fuji Eterna"),
            new("Fujifilm Eterna Vivid", 160, true, Ecn2, "Fujifilm Eterna Vivid", "Fuji Eterna Vivid"),
            new("Kentmere pan", null, false, null, "Kentmere pan", "Kentmere"),
            new("ORWO DN1", 0.25m, false, null, "ORWO DN1"),
            new("Ilford Ortho", 80, false, null, "Ilford Ortho"),
            new("Ilford HP5+", 400, false, null, "Ilford HP5+", "Ilford HP5", "ilford hp5 plus"),
            new("Ilford FP4+", 125, false, null, "Ilford FP4+", "Ilford FP4"),
            new("Kodak T-Max", 100, false, null, "Kodak T-Max", "kodak tmax"),
            new("Kodak E100", 100, true, "E-6", "Kodak E100"),
            new("Kodak Portra", 400, true, null, "Kodak Portra", "Portra"),
            new("Cyberpank", null, true, null, "Cyberpank", "Cyberpunk"),
            new("Славич Унибром ПЭ", 160, false, null, "Славич Унибром", "Slavich Unibrom"),
        };

        public static readonly CameraInfo[] Cameras =
        {
            new("Canon F-1", "35 мм", "Canon F-1", "F-1"),
            new("ЛК-А", "35 мм", "ЛК-А", "LCA", "LC-A", "Lomo LC-A"),
            new("ФЭД-2", "35 мм", "ФЭД-2", "FED2", "FED-2"),
            new("ФЭД Микрон-2", "35 мм", "ФЭД Микрон-2", "FED Mikron-2"),
            new("Зенит 3М", "35 мм", "Зенит 3М", "Zenit-3m", "Zenit 3M"),
            new("Смена 8М", "35 мм", "Смена 8М", "Smena8m", "Smena 8M"),
            // the 6x9 roll-film back; the plate back ("(БФ)") is chosen by hand
            new("Zeiss Ikon Maximar 207/1 (6x9)", "Средний 6x9", "Zeiss Ikon Maximar 207/1", "Zeiss Ikon Maximar", "Zeiss Ikon", "Zeiss"),
            new("Minoltaflex II", "Средний 6x6", "Minoltaflex II", "MinoltaflexII"),
            new("Зоркий 1", "35 мм", "Зоркий 1", "Zorki1", "Zorki 1"),
            new("Киев-19", "35 мм", "Киев-19", "Kiev-19"),
            new("Чайка 3", "35 мм", "Чайка 3", "chayka3"),
            new("Vera", "35 мм", "Vera"),
        };

        public static readonly (string Name, string[] Aliases)[] Developers =
        {
            ("Rodinal", new[] { "Rodinal", "FOMADON R09", "R09", "Adonal" }),   // R09 is a Rodinal formula (confirmed by the owner)
            ("AF Pyro-350", new[] { "AF Pyro-350", "AF pyro350", "pyro350", "PYRO 350", "пиро 350" }),
            ("ФГ-31П", new[] { "ФГ-31П", "ФГ31П" }),
            ("D-76", new[] { "D-76", "D76" }),
            (LabC41, new[] { "С-41", "C-41", "с41", "c41" }),
            ("E-6", new[] { "E-6", "e6" }),
            (Ecn2, new[] { "ECN-2", "ECN2" }),
            ("Moersch Tanol", new[] { "Moersch Tanol", "Moersh Tanol", "Tanol" }),
            (LabBw, new[] { "BW in lab" }),
        };

        /// <summary>Frames per strip in the negative album, by format (35 mm strips of 6; 120 strips of 3 for 6x6, 2 for 6x9).</summary>
        public static int FramesPerLine(string? format)
        {
            var key = Key(format);
            if (key.Contains("6x6")) return 3;
            if (key.Contains("6x9")) return 2;
            if (key.Contains("6x4")) return 4;
            if (key.Contains("6x7")) return 3;
            if (key == Key("Большой")) return 1;
            return 6;
        }

        /// <summary>Cameras that can only be set to certain film speeds.</summary>
        public static readonly Dictionary<string, decimal[]> EiSettings = new()
        {
            // LOMO LC-A film-speed dial (confirmed by the owner): no 125, so e.g. Aerocolor 125 is shot at 100
            ["ЛК-А"] = new decimal[] { 25, 50, 100, 200, 400 }
        };

        /// <summary>The camera's EI setting closest to <paramref name="ei"/> (the lower one on a tie).</summary>
        public static decimal NearestSetting(decimal[] settings, decimal ei) =>
            settings.OrderBy(s => Math.Abs(s - ei)).ThenBy(s => s).First();

        /// <summary>Comparison key: lower case, letters and digits only, Cyrillic look-alikes folded to Latin.</summary>
        public static string Key(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var sb = new StringBuilder(s.Length);
            var lower = s.ToLowerInvariant();
            for (var i = 0; i < lower.Length; i++)
            {
                var ch = lower[i];
                // keep a decimal point between digits ("ORWO DN1 0.25")
                if ((ch == '.' || ch == ',') && i > 0 && i < lower.Length - 1 && char.IsDigit(lower[i - 1]) && char.IsDigit(lower[i + 1]))
                {
                    sb.Append('.');
                    continue;
                }
                var c = ch switch
                {
                    'ё' => 'e', 'а' => 'a', 'в' => 'b', 'е' => 'e', 'к' => 'k', 'м' => 'm', 'н' => 'h',
                    'о' => 'o', 'р' => 'p', 'с' => 'c', 'т' => 't', 'у' => 'y', 'х' => 'x',
                    _ => ch
                };
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>The entry whose longest alias is a prefix of <paramref name="text"/>; returns the rest of the key.</summary>
        public static (T? Item, string Remainder) MatchPrefix<T>(string text, IEnumerable<T> items, Func<T, IEnumerable<string>> aliases) where T : class
        {
            var key = Key(text);
            T? best = null;
            var bestLen = 0;
            foreach (var item in items)
                foreach (var alias in aliases(item))
                {
                    var a = Key(alias);
                    if (a.Length > bestLen && key.StartsWith(a, StringComparison.Ordinal))
                    {
                        best = item;
                        bestLen = a.Length;
                    }
                }
            return (best, best == null ? key : key[bestLen..]);
        }

        public static CameraInfo? FindCamera(string text) => MatchPrefix(text, Cameras, c => c.Aliases.Append(c.Name)).Item;

        public static string? FindDeveloper(string text)
        {
            var (dev, _) = MatchPrefix(text, Developers.Select(d => new DevBox(d.Name, d.Aliases)), d => d.Aliases.Append(d.Name));
            return dev?.Name;
        }

        private record DevBox(string Name, string[] Aliases);
    }
}
