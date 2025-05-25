using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct EventWithSellerAndArticleCount(Event Event, Seller Seller, int ArticleCount);
