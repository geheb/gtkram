using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.MyBazaars;

public class ArticleInput
{
    public Guid? Id { get; set; }

    [Display(Name = "Artikelnummer")]
    public int LabelNumber { get; set; }

    [Display(Name = "Name", Prompt = "z.b. Schuhe")]
    [RequiredField]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Name { get; set; }

    [Display(Name = "Größe", Prompt = "z.b. 28")]
    [StringLength(7, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Size { get; set; }

    [Display(Name = "Preis in Euro")]
    [RequiredField]
    [DataType(DataType.Currency)]
    [Range(0.50, 500, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public decimal? Price { get; set; }

    public bool HasPriceClosestToFifty => Price.HasValue && Price.Value == Math.Ceiling(2 * Price.Value) / 2;

    public void To(BazaarSellerArticleDto dto)
    {
        dto.Id = Id;
        dto.Name = Name;
        dto.Size = Size;
        dto.Price = Price!.Value;
    }

    public void From(BazaarSellerArticleDto dto)
    {
        Id = dto.Id;
        LabelNumber = dto.LabelNumber;
        Name = dto.Name;
        Size = dto.Size;
        Price = dto.Price;
    }
}
