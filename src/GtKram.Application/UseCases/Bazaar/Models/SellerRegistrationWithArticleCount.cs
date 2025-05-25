using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct SellerRegistrationWithArticleCount(SellerRegistration Registration, Seller? Seller, int ArticleCount);

