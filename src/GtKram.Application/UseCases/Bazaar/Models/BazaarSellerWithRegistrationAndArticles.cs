using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerWithRegistrationAndArticles(BazaarSeller Seller, BazaarSellerRegistration Registration, BazaarSellerArticle[] Articles);
