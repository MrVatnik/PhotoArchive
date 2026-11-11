using PhotoArchive.Services.Import;

namespace PhotoArchive.Tests.Import
{
    public class ImportCatalogTests
    {
        [Theory]
        [InlineData("Canon F-1", "canon f1")]
        [InlineData("С-41", "C-41")]           // Cyrillic С vs Latin C
        [InlineData("Зенит 3М", "Зенит 3м")]
        [InlineData("AF Pyro-350", "af pyro 350")]
        [InlineData("ORWO DN1 0.25", "orwo dn1 0,25")]
        public void Key_ignores_case_punctuation_and_look_alikes(string a, string b)
        {
            Assert.Equal(ImportCatalog.Key(a), ImportCatalog.Key(b));
        }

        [Fact]
        public void Key_keeps_decimal_points_between_digits()
        {
            Assert.NotEqual(ImportCatalog.Key("ORWO DN1 0.25"), ImportCatalog.Key("ORWO DN1 025"));
        }

        [Theory]
        [InlineData("35 мм", 6)]
        [InlineData("Средний 6x6", 3)]
        [InlineData("Средний 6х6", 3)]   // Cyrillic х
        [InlineData("Средний 6x9", 2)]
        [InlineData("Большой", 1)]
        [InlineData(null, 6)]
        public void Frames_per_album_strip_follow_the_format(string? format, int frames)
        {
            Assert.Equal(frames, ImportCatalog.FramesPerLine(format));
        }

        [Theory]
        [InlineData(125, 100)]
        [InlineData(160, 200)]
        [InlineData(75, 50)]    // tie between 50 and 100: the lower one
        [InlineData(800, 400)]
        [InlineData(25, 25)]
        public void Lc_a_dial_snaps_to_nearest_setting(decimal ei, decimal expected)
        {
            Assert.Equal(expected, ImportCatalog.NearestSetting(ImportCatalog.EiSettings["ЛК-А"], ei));
        }

        [Theory]
        [InlineData("FOMADON R09", "Rodinal")]
        [InlineData("PYRO350", "AF Pyro-350")]
        [InlineData("пиро 350", "AF Pyro-350")]
        [InlineData("ФГ31п", "ФГ-31П")]
        [InlineData("с41", "С-41")]
        [InlineData("Some Developer", null)]
        public void Developer_aliases(string text, string? expected)
        {
            Assert.Equal(expected, ImportCatalog.FindDeveloper(text));
        }

        [Theory]
        [InlineData("canon f-1", "Canon F-1")]
        [InlineData("LCA", "ЛК-А")]
        [InlineData("Zeiss Ikon Maxomar 207/1", "Zeiss Ikon Maximar 207/1 (6x9)")]  // typo in the album index
        [InlineData("MinoltaflexII", "Minoltaflex II")]
        [InlineData("ФГ31П", null)]
        public void Camera_aliases(string text, string? expected)
        {
            Assert.Equal(expected, ImportCatalog.FindCamera(text)?.Name);
        }
    }
}
