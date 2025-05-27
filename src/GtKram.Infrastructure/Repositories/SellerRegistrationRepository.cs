using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories.Mappings;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrationRepository : ISellerRegistrationRepository
{
    private static readonly SemaphoreSlim _registerSemaphore = new SemaphoreSlim(1, 1);
    private readonly IRepository<Persistence.Entities.SellerRegistration> _repo;

    public SellerRegistrationRepository(IRepository<Persistence.Entities.SellerRegistration> repo)
    {
        _repo = repo;
    }

    public async Task<Result> Create(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        if (!await _registerSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerRegistration.Timeout);
        }

        try
        {
            var entity = model.MapToEntity(new(), new());
            await _repo.Create(entity, cancellationToken);
            return Result.Ok();
        }
        finally
        {
            _registerSemaphore.Release();
        }
    }

    public async Task<Result<Domain.Models.SellerRegistration>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entity.Value.Item.MapToDomain(new());
    }

    public async Task<Result<Domain.Models.SellerRegistration>> FindBySellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.SellerId, id)
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entities[0].Item.MapToDomain(new());
    }

    public async Task<Result<Domain.Models.SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.EventId, eventId),
                new(static e => e.Email, email),
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entities[0].Item.MapToDomain(new());
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.GetAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.Item.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken)
    {
        var entities = await _repo.GetAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Where(e => e.Item.Accepted == true).Select(e => e.Item.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.EventId, id),
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.Item.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.SellerRegistration>(ids.Length);
        var dc = new GermanDateTimeConverter();

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repo.Query(
                [
                    new(static e => e.SellerId, chunk),
                ],
                cancellationToken);

            result.AddRange(entities.Select(e => e.Item.MapToDomain(dc)));
        }

        return [.. result];
    }

    public async Task<Result> Update(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        model.MapToEntity(entity.Value.Item, new());
        var result = await _repo.Update(entity.Value.Item, cancellationToken);

        return result == UpdateResult.Success ? Result.Ok() : Result.Fail(Domain.Errors.SellerRegistration.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repo.Delete(id, cancellationToken);
        return result > 0 ? Result.Ok() : Result.Fail(Domain.Errors.SellerRegistration.SaveFailed);
    }

    public async Task<Result<int>> GetCountByEventId(Guid id, CancellationToken cancellationToken)
    {
        if (!await _registerSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerRegistration.Timeout);
        }

        try
        {
            var count = await _repo.Count(
                [
                    new(static e => e.EventId, id)
                ],
                cancellationToken);

            return Result.Ok(count);
        }
        finally
        {
            _registerSemaphore.Release();
        }
    }
}
