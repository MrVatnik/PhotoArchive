using System.ComponentModel.DataAnnotations;

namespace PhotoArchive.Enums
{
    public enum Format
    {

        [Display(Name = "35 мм")]
        mm35,

        [Display(Name = "Полукадр")]
        halfframe,

        [Display(Name = "СФ 6х9")]
        mid6x9
    }
}
