using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Plannings : IPlannings
{
    private readonly ISqlRepository<Planning, PlanningValues> _repository;

    public Plannings(
        ISqlRepository<Planning, PlanningValues> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.Planning model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new() { Json = new() });

        await _repository.Insert(entity, cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Planning model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Planning.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.Internal.ConflictData;
    }

    public async Task<Domain.Models.Planning[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Planning[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<ErrorOr<Domain.Models.Planning>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        return entity is null ? Domain.Errors.Planning.NotFound : entity.MapToDomain(new());
    }
}
