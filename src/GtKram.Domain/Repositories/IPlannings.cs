using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IPlannings
{
    Task<ErrorOr<Success>> Create(Planning model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Planning model, CancellationToken cancellationToken);
    Task<ErrorOr<Planning>> Find(Guid id, CancellationToken cancellationToken);
    Task<Planning[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<Planning[]> GetAll(CancellationToken cancellationToken);
}
