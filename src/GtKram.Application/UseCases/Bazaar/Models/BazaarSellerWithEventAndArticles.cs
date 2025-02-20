using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerWithEventAndArticles(BazaarSeller Seller, BazaarEvent Event, BazaarSellerArticleWithBilling[] Articles);