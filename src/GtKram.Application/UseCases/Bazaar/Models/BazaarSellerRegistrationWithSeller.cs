using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed record BazaarSellerRegistrationWithSeller(BazaarSellerRegistration Registration, BazaarSeller? Seller);
