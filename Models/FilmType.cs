using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoArchive.Models
{
    public class FilmType
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal ISO {get; set; }

        public bool Color_Is_Possible { get; set; }

        public override string ToString()
        {
            return Name + " "  + ISO;
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
