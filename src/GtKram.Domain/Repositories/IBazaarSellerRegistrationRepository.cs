using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerRegistrationRepository
{
    Task<Result> Create(BazaarSellerRegistration model, CancellationToken cancellationToken);
    Task<Result<BazaarSellerRegistration>> Find(Guid id, CancellationToken cancellationToken);
    Task<BazaarSellerRegistration[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSellerRegistration model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, int>> GetCountByBazaarEventId(CancellationToken cancellationToken);
}
