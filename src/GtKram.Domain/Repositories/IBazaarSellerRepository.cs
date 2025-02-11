using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerRepository
{
    Task<Result<Guid>> Create(BazaarSeller model, Guid userId, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> Find(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetByUserId(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSeller model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
}
