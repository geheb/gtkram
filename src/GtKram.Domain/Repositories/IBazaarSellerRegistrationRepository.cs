using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerRegistrationRepository
{
    Task<Result> Create(BazaarSellerRegistration model, CancellationToken cancellationToken);
    Task<Result<BazaarSellerRegistration>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarSellerRegistration>> FindByBazaarSellerId(Guid id, CancellationToken cancellationToken);
    Task<BazaarSellerRegistration[]> GetAll(CancellationToken cancellationToken);
    Task<BazaarSellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken);
    Task<BazaarSellerRegistration[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<BazaarSellerRegistration[]> GetByBazaarSellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSellerRegistration model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result<int>> GetCountByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarSellerRegistration>> FindByEmail(string email, CancellationToken cancellationToken);
}
