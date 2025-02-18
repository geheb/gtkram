using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerArticleWithBilling(
    BazaarSellerArticle SellerArticle, 
    BazaarBillingArticle BillingArticle,
    int SellerNumber)
{
    public string Format() =>
        $"{SellerArticle.Name} #{SellerArticle.LabelNumber} für {SellerArticle.Price:0.00} € (Verkäufernummer {SellerNumber})";
}
