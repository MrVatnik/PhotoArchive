using System.IO.Compression;
using System.Security;
using System.Text;
using SkiaSharp;

namespace PhotoArchive.Tests
{
    /// <summary>Small helpers that create throw-away files for the tests.</summary>
    public static class TestFiles
    {
        public static string NewTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "PhotoArchiveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void DeleteDir(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch (IOException) { /* a file may still be open on Windows; it's in %TEMP% anyway */ }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>A real, decodable JPEG of the given size.</summary>
        public static byte[] Jpeg(int width = 300, int height = 200, SKColor? color = null)
        {
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
                canvas.Clear(color ?? SKColors.SteelBlue);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            return data.ToArray();
        }

        public static void WriteJpeg(string path, int width = 300, int height = 200)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, Jpeg(width, height));
        }

        /// <summary>A minimal .docx whose first table has the given rows (like "Плёнки описание.docx").</summary>
        public static void WriteAlbumDocx(string path, params string[][] rows)
        {
            var body = new StringBuilder();
            if (rows.Length > 0)
            {
                body.Append("<w:tbl>");
                foreach (var row in rows)
                {
                    body.Append("<w:tr>");
                    foreach (var cell in row)
                        body.Append($"<w:tc><w:p><w:r><w:t>{SecurityElement.Escape(cell)}</w:t></w:r></w:p></w:tc>");
                    body.Append("</w:tr>");
                }
                body.Append("</w:tbl>");
            }
            body.Append("<w:p><w:r><w:t>caption tables would follow</w:t></w:r></w:p>");

            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>"
                + body + "</w:body></w:document>";

            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            using var writer = new StreamWriter(zip.CreateEntry("word/document.xml").Open(), new UTF8Encoding(false));
            writer.Write(xml);
        }
    }
}
