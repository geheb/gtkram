using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ICheckouts
{
    Task<ErrorOr<Guid>> Create(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Checkout[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<Checkout[]> GetByEventIdAndUserId(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Checkout[]> GetByIdentityId(Guid id, CancellationToken cancellationToken);
    Task<Checkout[]> GetAll(CancellationToken cancellationToken);
    Task<Checkout[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<ErrorOr<Checkout>> Find(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Checkout model, CancellationToken cancellationToken);
    Task<bool> HasArticle(Guid eventId, Guid articleId, CancellationToken cancellationToken);
}
