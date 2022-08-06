using System.ComponentModel.DataAnnotations;

namespace PhotoArchive.Enums
{
    public enum Camera
    {
        [Display(Name = "Canon F-1")]
        CanonF1,

        [Display(Name = "Ломо ЛК-А")]
        LOMOLCA,

        [Display(Name = "ФЭД 2")]
        FED2,

        [Display(Name = "Zeiss Ikon Maximar 207/1")]
        ZeissIkonMaximar2071,

        [Display(Name = "Зенит 3М")]
        Zenit3m,

        [Display(Name = "Смена 8М")]
        Smena8m

    }
}
