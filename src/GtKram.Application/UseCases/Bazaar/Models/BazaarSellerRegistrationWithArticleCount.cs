using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerRegistrationWithArticleCount(BazaarSellerRegistration Registration, BazaarSeller? Seller, int ArticleCount);

