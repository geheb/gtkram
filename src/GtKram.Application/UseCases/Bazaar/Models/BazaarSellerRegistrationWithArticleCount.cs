using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed record BazaarSellerRegistrationWithArticleCount(BazaarSellerRegistration Registration, BazaarSeller? Seller, int ArticleCount);

