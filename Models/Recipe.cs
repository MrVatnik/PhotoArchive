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

        public decimal EI { get; set; }

        public int FilmTypeId { get; set; }
        [DisplayName("Type of Film")]
        public FilmType? FilmType { get; set; }

        public int DeveloperId { get; set; }

        public Developer? Developer { get; set; }

        [DisplayName("Time:Minutes")]
        public int Min { get; set; }
        [DisplayName("Seconds")]
        public int Sec { get; set; }

        public bool Color {get; set;}

        public override string ToString()
        {
            if (Math.Truncate(EI) > 0 || Math.Truncate(EI) < 0)
                return FilmType + " in " + Developer + " as " + (int)EI + " EI";
            else
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
