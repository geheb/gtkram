using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct SellerRegistrationWithSeller(SellerRegistration Registration, Seller? Seller);
