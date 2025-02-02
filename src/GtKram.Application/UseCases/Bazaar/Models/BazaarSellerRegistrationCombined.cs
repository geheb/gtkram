using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed record BazaarSellerRegistrationCombined(BazaarSellerRegistration Registration, BazaarSeller? Seller);
