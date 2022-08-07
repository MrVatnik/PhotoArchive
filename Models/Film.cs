using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoArchive.Models
{
    public class Film
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [DisplayName("Id")]
        public int Id { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        public int CameraId { get; set; }
        public Camera? Camera { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public List<Photo>? Photos { get; set; }

        public string FolderName { get; set; }

        public override string ToString()
        {
            
            if(Recipe == null)
                return "" + Date.ToString("yy-MM-dd") + " хз " + Camera;
            else
                return "" + Date.ToString("yy-MM-dd") + (Recipe.Color ? " цв " : " чб ") + Camera;
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
