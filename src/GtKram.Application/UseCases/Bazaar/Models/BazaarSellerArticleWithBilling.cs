using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerArticleWithBilling(
    BazaarSellerArticle SellerArticle, 
    BazaarBillingArticle BillingArticle,
    int SellerNumber);
