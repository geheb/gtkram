using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerArticleWithEvent(BazaarSellerArticle Article, BazaarEvent Event, bool IsBooked);