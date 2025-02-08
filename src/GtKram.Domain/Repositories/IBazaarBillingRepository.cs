using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarBillingRepository
{
    Task<Result> Create(BazaarBilling model, CancellationToken cancellationToken);
    Task<BazaarBilling[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
}
