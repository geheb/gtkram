using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories.Mappings;

namespace GtKram.Infrastructure.Repositories;

internal sealed class CheckoutRepository : ICheckoutRepository
{
    private readonly IRepository<Persistence.Entities.Checkout> _repo;

    public CheckoutRepository(IRepository<Persistence.Entities.Checkout> repo)
    {
        _repo = repo;
    }

    public async Task<Result<Guid>> Create(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var entity = new Persistence.Entities.Checkout
        {
            EventId = eventId,
            UserId = userId,
            Status = (int)CheckoutStatus.InProgress
        };

        await _repo.Create(entity, null, cancellationToken);

        return Result.Ok(entity.Id);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repo.Delete(id, null, cancellationToken);

        return affectedRows > 0 ? Result.Ok() : Result.Fail(Domain.Errors.Checkout.SaveFailed);
    }

    public async Task<Result<Domain.Models.Checkout>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, null, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        return entity.Value.MapToDomain(new());
    }

    public async Task<Domain.Models.Checkout[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.Get(null, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.EventId, id)],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventIdAndUserId(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.EventId, eventId),
                new(static e => e.UserId, userId)
            ],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var entities = await _repo.Get(
            ids,
            null,
            cancellationToken);

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByUserId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.UserId, id)],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<bool> HasArticle(Guid eventId, Guid articleId, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.EventId, eventId)
            ],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return false;
        }

        return entities.Any(e => e.Item.ArticleIds.Contains(articleId));
    }

    public async Task<Result> Update(Domain.Models.Checkout model, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(model.Id, null, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        model.MapToEntity(entity.Value.Item);
        var result = await _repo.Update(entity.Value.Item, null, cancellationToken);

        return result == UpdateResult.Success ? Result.Ok() : Result.Fail(Domain.Errors.Checkout.SaveFailed);
    }
}
