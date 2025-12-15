using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories.Mappings;

namespace GtKram.Infrastructure.Repositories;

internal sealed class EventRepository : IEventRepository
{
    private readonly IRepository<Database.Entities.Event> _repo;

    public EventRepository(IRepository<Database.Entities.Event> repo)
    {
        _repo = repo;
    }

    public async Task<Result<Guid>> Create(Domain.Models.Event model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new(), new());
        entity.Commission = 20;

        await _repo.Create(entity, cancellationToken);

        return Result.Ok(entity.Id);
    }

    public async Task<Result<Domain.Models.Event>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Event.NotFound);
        }

        return entity.Value.Item.MapToDomain(new());
    }

    public async Task<Domain.Models.Event[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repo.Get(ids, cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.Item.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Event[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.GetAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.Item.MapToDomain(dc)).OrderByDescending(e => e.Start)];
    }

    public async Task<Result> Update(Domain.Models.Event model, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Event.NotFound);
        }

        model.MapToEntity(entity.Value.Item, new());
        var result = await _repo.Update(entity.Value.Item, cancellationToken);

        return result == UpdateResult.Success ? Result.Ok() : Result.Fail(Domain.Errors.Event.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repo.Delete(id, cancellationToken);
        return result > 0 ? Result.Ok() : Result.Fail(Domain.Errors.Event.DeleteFailed);
    }
}
