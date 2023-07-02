using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace PhotoArchive.Models
{
    public class Photo
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int Id { get; set;}

        public string? Name { get; set; }

        [DisplayName("Picture")]
        public string? Pic  { get; set; }

        
        /*[DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }*/

        public int FilmId { get; set; }
        public Film? Film { get; set; }

        [DisplayName("Is Liked")]
        public bool Is_Liked { get; set; }

        public int Page { get; set; }

        public int Line { get; set; }

        [DisplayName("Place in Line")]
        public int Place_In_Line { get; set; }


        public string Is_LikedString
        {
            get
            {
                string res = this.Is_Liked + ".png";
                return res;
            }
        }

        public string? Miniature()
        {
            string MiniatureAdress = "/Images/Films/Miniatures/" + Id + ".jpg";
            string miniatureFilePath = Path.Combine(Environment.CurrentDirectory, @"wwwroot\Images\Films\Miniatures\" + Id + ".jpg");
            if (!File.Exists(miniatureFilePath))
            {
                string originalPath = Path.Combine(Environment.CurrentDirectory, @"wwwroot/Images/Films/Films/" + Film.FolderName + "/" + this.Pic);

                if (!File.Exists(originalPath))
                {
                    originalPath = Path.Combine(Environment.CurrentDirectory, @"wwwroot/Images/No_image.JPG");
                }
                
                
                Image image = Image.FromFile(originalPath);
                int width;
                int height;

                if (image.Height <= image.Width)
                {
                    width = 175;
                    height = (int)((float)image.Height * (float)(175) / (float)image.Width);
                }
                else
                {
                    height = 175;
                    width = (int)((float)image.Width * (float)(175) / (float)image.Height);
                }

                Image res = ResizeImage(image, width, height);
                res.Save(miniatureFilePath, ImageFormat.Jpeg);
            }
            return MiniatureAdress;
        }

        public string? Maximized()
        {
            string MaximizedAdress = "Images/Films/Maximized/" + Id + ".jpg";
            return MaximizedAdress;
            
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

    }
}
