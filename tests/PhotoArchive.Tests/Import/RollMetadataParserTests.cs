using PhotoArchive.Services.Import;

namespace PhotoArchive.Tests.Import
{
    /// <summary>Real "Пленка+Аппарат.txt" lines from the archive and what the importer must make of them.</summary>
    public class RollMetadataParserTests
    {
        public record Case(string Folder, string? Txt, string Stock, decimal? Iso, decimal? Ei, string Developer, string Camera)
        {
            public override string ToString() => Folder;
        }

        public static TheoryData<Case> Cases => new()
        {
            new("21-05-30_bw_FED2", "Foma pan 400 + ФЭД-2", "Foma pan", 400, 400, "BW in lab", "ФЭД-2"),
            new("21-06-05_col_FED2", "fujicolor c200 + ФЭД-2\r\n", "Fujifilm C", 200, 200, "С-41", "ФЭД-2"),
            // Rollei Superpan 200 = Agfa Aviphot 200, FOMADON R09 = Rodinal
            new("21-11-07_bw_F-1", "Rollei Superpan 200 + FOMADON R09 + canon f-1", "Agfa Aviphot", 200, 200, "Rodinal", "Canon F-1"),
            new("22-04-15_bw_F-1", "Aviphot как 90 + AF pyro350 + canon f-1", "Agfa Aviphot", 200, 90, "AF Pyro-350", "Canon F-1"),
            // lens after the camera
            new("23-01-22_BW_Zorki1", "aviphot as 800 + ФГ31П + Зоркий 1 + Юпитер 12", "Agfa Aviphot", 200, 800, "ФГ-31П", "Зоркий 1"),
            // motion-picture stocks default to ECN-2; d/t suffix picks the variant
            new("22-12-19_col_F-1", "Kodak Vision 3 250d as 320 + canon f-1", "Kodak Vision3 d", 250, 320, "ECN-2", "Canon F-1"),
            new("22-02-11_col_F-1", "Kodak vision3 500t + canon f-1", "Kodak Vision3 t", 500, 500, "ECN-2", "Canon F-1"),
            new("22-04-30_col_F-1", "fuji Eterna 250D as 100 EI + canon f-1", "Fujifilm Eterna", 250, 100, "ECN-2", "Canon F-1"),
            new("22-11-06_col_F-1", "Fuji Eterna Vivid 160 as 100 + canon f-1", "Fujifilm Eterna Vivid", 160, 100, "ECN-2", "Canon F-1"),
            // decimal box speed
            new("22-04-04_bw_F-1", "ORWO DN1 0.25 + canon f-1", "ORWO DN1", 0.25m, 0.25m, "BW in lab", "Canon F-1"),
            // "+" inside a stock name, and an empty part between two pluses
            new("23-09-02_BW_MinoltaflexII", "Ilford FP4+ + ФГ31П  + Minoltaflex II", "Ilford FP4+", 125, 125, "ФГ-31П", "Minoltaflex II"),
            new("22-01-14_bw_Zeiss Ikon Maximar", "Fomapan 200 + FOMADON R09 + + Zeiss Ikon Maximar 207/1", "Foma pan", 200, 200, "Rodinal", "Zeiss Ikon Maximar 207/1 (6x9)"),
            new("22-02-20_bw_zeiss", "Fomapan 400 + FOMADON R09 + Zeiss Ikon", "Foma pan", 400, 400, "Rodinal", "Zeiss Ikon Maximar 207/1 (6x9)"),
            // LC-A dial: 25/50/100/200/400 — 125 snaps to 100, 200 stays 200, 25 stays 25
            new("21-09-15_col_LCA", "Kodak Aerocolor 125 + ЛК-А", "Kodak Aerocolor", 125, 100, "С-41", "ЛК-А"),
            new("23-06-12_col_LCA", "Kodak Gold 200 + ЛК-А\r\nстр 67", "Kodak gold", 200, 200, "С-41", "ЛК-А"),
            new("22-09-11_bw_LCA", "aviphot as 100 + AF pyro350  + ЛК-А", "Agfa Aviphot", 200, 100, "AF Pyro-350", "ЛК-А"),
            // КН-2: box speed unknown, ISO taken from the EI
            new("22-07-29_bw_LCA", "кн2 как 25 + AF pyro350 + ЛК-А", "КН-2", 25, 25, "AF Pyro-350", "ЛК-А"),
            // Silberra Color = Kodak Aerocolor
            new("22-06-01_col_F-1", "Silberra color + canon f-1", "Kodak Aerocolor", 125, 125, "С-41", "Canon F-1"),
            // explicit lab developers
            new("23-06-26_col_MinoltaflexII", "kodak aerocoloar +  e6 + Minoltaflex II\r\nстр 70", "Kodak Aerocolor", 125, 125, "E-6", "Minoltaflex II"),
            new("23-09-10_bw_Zeiss Ikon Maximar", "kodak aerocolor +  с41 + Zeiss Ikon Maximar 207/1\nстр 79", "Kodak Aerocolor", 125, 125, "С-41", "Zeiss Ikon Maximar 207/1 (6x9)"),
            // nested brackets are notes
            new("23-12-12_col_F-1", "Cyberpank 640T (китайская копия Cinestill 800T (Kodak visionn 3 500T со смытой сажей)) + canon f-1", "Cyberpank", 640, 640, "С-41", "Canon F-1"),
            // EI range: first value, range kept as a note; bracket note on the developer
            new("25-03-23_BW_MinoltaflexII", "aviphot as 50-100 + Moersh Tanol (pyrocat-hd)  + Minoltaflex II", "Agfa Aviphot", 200, 50, "Moersch Tanol", "Minoltaflex II"),
            new("21-10-14_bw_F-1", "Silberra ultima100 + D76 + canon f-1", "Silberra ultima", 100, 100, "D-76", "Canon F-1"),
            new("21-12-07_bw_Zeiss Ikon Maximar", "Славич Унибром 160 ПЭ \"Берёзка\" + Zeiss Ikon Maximar 207/1", "Славич Унибром ПЭ", 160, 160, "BW in lab", "Zeiss Ikon Maximar 207/1 (6x9)"),
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public void Parses_archive_notes(Case c)
        {
            var m = RollMetadataParser.Parse(c.Folder, c.Txt);

            Assert.Equal(c.Stock, m.Stock);
            Assert.Equal(c.Iso, m.Iso);
            Assert.Equal(c.Ei, m.Ei);
            Assert.Equal(c.Developer, m.Developer);
            Assert.Equal(c.Camera, m.Camera);
            Assert.DoesNotContain(m.Warnings, w => w.StartsWith("Unknown film stock"));
        }

        [Fact]
        public void Folder_name_gives_date_colour_and_camera_hint()
        {
            var m = RollMetadataParser.Parse("23-10-19_bw_Kiev-19", null);

            Assert.Equal(new DateTime(2023, 10, 19), m.Date);
            Assert.False(m.Color);
            Assert.Equal("Киев-19", m.Camera);
            Assert.Contains("No metadata .txt", m.Warnings);
        }

        [Fact]
        public void Album_page_lens_and_ranges_become_notes_or_fields()
        {
            var zorki = RollMetadataParser.Parse("23-03-23_bw_Zorki1", "aviphot as 400 + PYRO350 + Зоркий 1 + Юпитер 12 \r\nстр 61");
            Assert.Equal(61, zorki.AlbumPage);
            Assert.Contains(zorki.Notes, n => n.Contains("Юпитер 12"));

            Assert.Equal(89, RollMetadataParser.Parse("23-10-09_col_LCA", "Kodak Gold 200 + ЛК-А\r\nстр -89").AlbumPage);
            Assert.Equal(73, RollMetadataParser.Parse("23-07-25_BW_MinoltaflexII", "aviphot as 200 + AF pyro350  + Minoltaflex II\r\nстр 73-74").AlbumPage);

            var range = RollMetadataParser.Parse("25-03-23_BW_MinoltaflexII", "aviphot as 50-100 + Moersh Tanol (pyrocat-hd)  + Minoltaflex II");
            Assert.Contains(range.Notes, n => n.Contains("50-100"));
            Assert.Contains(range.Notes, n => n == "pyrocat-hd");
        }

        [Fact]
        public void Lc_a_note_explains_snapped_ei()
        {
            var m = RollMetadataParser.Parse("21-10-15_col_LCA", "Kodak Aerocolor 125 + ЛК-А");
            Assert.Contains(m.Notes, n => n.Contains("no EI 125"));
        }

        [Theory]
        [InlineData("21-05-30_bw_FED2", true)]
        [InlineData("23-06-25_BW_MinoltaflexII-RESCAN", true)]
        [InlineData("21-12-19_col_Zeiss_Ikon_Maximar", true)]
        [InlineData("папа маленький", false)]
        [InlineData("Export", false)]
        public void Recognises_roll_folders(string folder, bool isRoll)
        {
            Assert.Equal(isRoll, RollMetadataParser.IsRollFolder(folder));
        }

        [Fact]
        public void Rescan_folders_are_flagged()
        {
            Assert.True(RollMetadataParser.Parse("23-06-25_BW_MinoltaflexII-RESCAN", "kodak tmax 100 +  ФГ31П  + Minoltaflex II").IsRescan);
            Assert.False(RollMetadataParser.Parse("23-06-25_BW_MinoltaflexII", "kodak tmax 100 +  ФГ31П  + Minoltaflex II").IsRescan);
        }

        [Fact]
        public void Unknown_stock_is_kept_and_flagged()
        {
            var m = RollMetadataParser.Parse("24-01-01_bw_F-1", "Mystery Film 320 + canon f-1");
            Assert.Equal("Mystery Film", m.Stock);
            Assert.Equal(320, m.Iso);
            Assert.Contains(m.Warnings, w => w.StartsWith("Unknown film stock"));
        }
    }
}
