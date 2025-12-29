using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct SellerWithEventAndArticles(Seller Seller, Event Event, ArticleWithCheckout[] Articles);