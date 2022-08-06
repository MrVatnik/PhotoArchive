using PhotoArchive.Enums;
using System.ComponentModel.DataAnnotations;

namespace PhotoArchive.Models
{
    public class Film
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Camera Camera { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public List<Photo>? Photos { get; set; }

        public bool Color { get; set; }

        public string FolderName { get; set; }

        public override string ToString()
        {
            return "" + Date.ToString("yy-MM-dd") + (Color ? " цв " : " чб ") + Camera.GetDisplayName();
        }
    }
}
