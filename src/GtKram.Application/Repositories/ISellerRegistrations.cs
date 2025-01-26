using GtKram.Application.UseCases.Bazaar.Models;

namespace GtKram.Application.Repositories;

public interface ISellerRegistrations
{
    Task<BazaarSellerRegistrationDto[]> GetAll(Guid eventId, CancellationToken cancellationToken);
    Task<bool> Confirm(Guid eventId, Guid registrationId, bool confirmed, string? registerUserCallbackUrl, CancellationToken cancellationToken);
    Task<bool> Delete(Guid eventId, Guid sellerId, CancellationToken cancellationToken);
    Task<Guid?> Register(Guid eventId, BazaarSellerRegistrationDto dto, CancellationToken cancellationToken);
    Task<bool> Register(Guid eventId, string email, string name, string phone, CancellationToken cancellationToken);
}
