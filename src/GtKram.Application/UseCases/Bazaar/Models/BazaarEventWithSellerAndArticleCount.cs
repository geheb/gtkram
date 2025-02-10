using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarEventWithSellerAndArticleCount(BazaarEvent Event, BazaarSeller Seller, int ArticleCount);
