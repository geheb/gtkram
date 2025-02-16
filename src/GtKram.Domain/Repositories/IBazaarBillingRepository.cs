using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarBillingRepository
{
    Task<Result> Create(BazaarBilling model, CancellationToken cancellationToken);
    Task<BazaarBilling[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<BazaarBilling[]> GetByUserId(Guid id, CancellationToken cancellationToken);
    Task<BazaarBilling[]> GetAll(CancellationToken cancellationToken);
    Task<Result<BazaarBilling>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(BazaarBilling model, CancellationToken cancellationToken);
}
