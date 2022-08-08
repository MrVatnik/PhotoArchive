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

    }
}
