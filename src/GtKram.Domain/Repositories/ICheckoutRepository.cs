using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ICheckoutRepository
{
    Task<Result<Guid>> Create(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Checkout[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<Checkout[]> GetByEventIdAndUserId(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<Checkout[]> GetByUserId(Guid id, CancellationToken cancellationToken);
    Task<Checkout[]> GetAll(CancellationToken cancellationToken);
    Task<Checkout[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Result<Checkout>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(Checkout model, CancellationToken cancellationToken);
    Task<bool> HasArticle(Guid eventId, Guid articleId, CancellationToken cancellationToken);
}
