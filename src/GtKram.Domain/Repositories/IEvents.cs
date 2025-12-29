using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IEvents
{
    Task<ErrorOr<Guid>> Create(Event model, CancellationToken cancellationToken);
    Task<ErrorOr<Event>> Find(Guid id, CancellationToken cancellationToken);
    Task<Event[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Event[]> GetAll(CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Event model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken);
}
