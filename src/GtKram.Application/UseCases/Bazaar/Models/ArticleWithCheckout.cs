using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct ArticleWithCheckout(
    Article Article,
    Checkout? Checkout,
    int SellerNumber)
{
    public string Format() =>
        $"{Article.Name} #{Article.LabelNumber} für {Article.Price:0.00} € (Verkäufernummer {SellerNumber})";
}
