using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.MyBazaars;

public sealed class ArticleInput
{
    public string State_Event { get; set; } = "Unbekannt";
    public string? State_EditArticleEndDate { get; set; }

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

    public void Init(Article model)
    {
        LabelNumber = model.LabelNumber;
        Name = model.Name;
        Size = model.Size;
        Price = model.Price;
    }

    internal CreateArticleByUserCommand ToCreateCommand(Guid userId, Guid sellerId) =>
        new(userId, sellerId, Name!, Size!, Price!.Value);

    internal UpdateArticleByUserCommand ToUpdateCommand(Guid userId, Guid articleId) =>
        new(userId, articleId, Name!, Size!, Price!.Value);
}
