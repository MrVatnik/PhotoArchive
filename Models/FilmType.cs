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

        [DisplayName("Is Color possible?")] 
        public bool Color_Is_Possible { get; set; }

        public override string ToString()
        {
            if (Math.Truncate(ISO) > 0 || Math.Truncate(ISO) < 0)
                return Name + " " + (int)ISO;
            else
                return Name + " " + ISO;
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
