using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoArchive.Models
{
    public class Developer
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int Id { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
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
