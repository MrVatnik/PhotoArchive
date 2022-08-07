using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoArchive.Models
{
    public class Recipe
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int Id { get; set; }

        public int EI { get; set; }

        public int FilmTypeId { get; set; }
        public FilmType? FilmType { get; set; }

        public int DeveloperId { get; set; }

        public Developer? Developer { get; set; }


        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:mmm:ss}", ApplyFormatInEditMode = true)]
        public DateTime Time { get; set; }

        public bool Color {get; set;}

        public override string ToString()
        {
            return FilmType + " in " + Developer + " as " + EI + " EI";
        }
        public string To_String
        {
            get
            {
                return ToString();
            }
        }
    }
}
