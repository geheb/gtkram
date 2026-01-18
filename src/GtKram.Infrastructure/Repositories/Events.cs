using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Events : IEvents
{
    private readonly ISqlRepository<Event> _repository;

    public Events(ISqlRepository<Event> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Guid>> Create(Domain.Models.Event model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new() { Json = new() });
        entity.Json.Commission = 20;

        await _repository.Insert(entity, cancellationToken);

        return entity.Id;
    }

    public async Task<ErrorOr<Domain.Models.Event>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.Event.NotFound;
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.Event[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectMany(ids, cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Event[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Event model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Event.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.Internal.ConflictData;
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repository.Delete(id, cancellationToken);
        return result > 0 ? Result.Success : Domain.Errors.Event.DeleteFailed;
    }
}
