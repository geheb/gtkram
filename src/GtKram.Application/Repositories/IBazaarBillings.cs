using GtKram.Application.UseCases.Bazaar.Models;

namespace GtKram.Application.Repositories;

public interface IBazaarBillings
{
    Task<BazaarBillingDto[]> GetAll(Guid eventId, CancellationToken cancellationToken);
    Task<BazaarBillingDto[]> GetAll(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<Guid> Create(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<BazaarBillingDto?> Find(Guid eventId, Guid billingId, CancellationToken cancellationToken);
    Task<bool> SetAsCompleted(Guid eventId, Guid billingId, CancellationToken cancellationToken);
}
