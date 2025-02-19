using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.MyBillings;

public sealed class ArticleInput
{
    public string? State_Event { get; set; }

    [Display(Name = "Verkäufernummer")]
    [RequiredField]
    [Range(1, 999, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int? SellerNumber { get; set; }

    [Display(Name = "Artikelnummer")]
    [RequiredField]
    [Range(1, 999, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int? LabelNumber { get; set; }
}
