using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarEventRepository
{
    Task<Result> Create(BazaarEvent model, CancellationToken cancellationToken);
    Task<Result<BazaarEvent>> Find(Guid id, CancellationToken cancellationToken);
    Task<BazaarEvent[]> Get(Guid[] ids, CancellationToken cancellationToken);
    Task<BazaarEvent[]> GetAll(CancellationToken cancellationToken);
    Task<Result> Update(BazaarEvent model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
}
