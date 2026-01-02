using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ISellers
{
    Task<ErrorOr<Guid>> Create(Seller model, CancellationToken cancellationToken);
    Task<ErrorOr<Seller>> Find(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetByIdentityId(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Seller>> FindByIdentityIdAndEventId(Guid identityId, Guid eventId, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Seller model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken);
    Task<Seller[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<ErrorOr<Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken);
}
