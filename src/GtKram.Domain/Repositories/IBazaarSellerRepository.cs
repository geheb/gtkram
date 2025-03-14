using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerRepository
{
    Task<Result<Guid>> Create(BazaarSeller model, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> Find(Guid id, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetByUserId(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> GetByUserIdAndBazaarEventId(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSeller model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetAll(CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> FindBySellerNumberAndEventId(int sellerNumber, Guid eventId, CancellationToken cancellationToken);
}
