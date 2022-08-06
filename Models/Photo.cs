using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoArchive.Models
{
    public class Photo
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int id { get; set;}

        public string Name { get; set; }

        public string Pic { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public bool Is_Liked { get; set; }

        public int Page { get; set; }

        public int Line { get; set; }

        public int Place_In_Line { get; set; }

        public Formats Format { get; set; }

    }
}
