using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarSellerRegistrationWithSeller(BazaarSellerRegistration Registration, BazaarSeller? Seller);
