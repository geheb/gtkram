using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IEvents
{
    Task<Result<Guid>> Create(Event model, CancellationToken cancellationToken);
    Task<Result<Event>> Find(Guid id, CancellationToken cancellationToken);
    Task<Event[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Event[]> GetAll(CancellationToken cancellationToken);
    Task<Result> Update(Event model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
}
