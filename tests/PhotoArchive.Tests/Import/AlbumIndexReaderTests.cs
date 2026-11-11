using PhotoArchive.Services.Import;

namespace PhotoArchive.Tests.Import
{
    public class AlbumIndexReaderTests : IDisposable
    {
        private readonly string _dir = TestFiles.NewTempDir();

        public void Dispose() => TestFiles.DeleteDir(_dir);

        [Fact]
        public void Reads_the_roll_index_table()
        {
            var path = Path.Combine(_dir, "album.docx");
            TestFiles.WriteAlbumDocx(path,
                new[] { "Адрес", "", "Плёнка", "Аппарат", "Дата ~" },
                new[] { "11 – 1", "11 – 7", "Silberra ultima 100", "Canon F-1", "14.10.21 (30.11)" },
                new[] { "29 – 7", "30 – 7", "Aviphot как 90", "Canon F-1", "15.04.22" },
                new[] { "33 – 4", "33 – 6", "Aviphot как 90", "ФЭД Микрон-2", "Апрель 22" },
                new[] { "37 – 1", "37 – 7", "", "", "" });   // empty template row

            var rolls = AlbumIndexReader.Read(path);

            Assert.Equal(3, rolls.Count);
            var first = rolls[0];
            Assert.Equal((11, 1, 11, 7), (first.StartPage, first.StartLine, first.EndPage, first.EndLine));
            Assert.Equal("Silberra ultima 100", first.Stock);
            Assert.Equal(new[] { new DateTime(2021, 10, 14), new DateTime(2021, 11, 30) }, first.Dates);
            Assert.Equal("29-7..30-7", rolls[1].Address);
            Assert.Empty(rolls[2].Dates);   // "Апрель 22" has no day
        }

        [Fact]
        public void Document_without_tables_gives_no_rolls()
        {
            var path = Path.Combine(_dir, "empty.docx");
            TestFiles.WriteAlbumDocx(path);
            Assert.Empty(AlbumIndexReader.Read(path));
        }
    }
}
