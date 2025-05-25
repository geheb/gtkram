using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct SellerWithRegistrationAndArticles(Seller Seller, SellerRegistration Registration, ArticleWithCheckout[] Articles);
