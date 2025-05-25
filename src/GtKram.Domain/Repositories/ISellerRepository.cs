using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ISellerRepository
{
    Task<Result<Guid>> Create(Seller model, CancellationToken cancellationToken);
    Task<Result<Seller>> Find(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetByUserId(Guid id, CancellationToken cancellationToken);
    Task<Result<Seller>> GetByUserIdAndEventId(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<Result> Update(Seller model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetAll(CancellationToken cancellationToken);
    Task<Seller[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Result<Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken);
}
