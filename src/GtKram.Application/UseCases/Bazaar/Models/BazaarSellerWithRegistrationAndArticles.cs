using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed record BazaarSellerWithRegistrationAndArticles(BazaarSeller Seller, BazaarSellerRegistration Registration, BazaarSellerArticle[] Articles);
