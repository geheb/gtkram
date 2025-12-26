using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Checkouts : ICheckouts
{
    private readonly ISqlRepository<Checkout> _repository;

    public Checkouts(ISqlRepository<Checkout> repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Create(Guid eventId, Guid identityId, CancellationToken cancellationToken)
    {
        var entity = new Checkout
        {
            Json = new()
            {
                EventId = eventId,
                IdentityId = identityId,
                Status = (int)Domain.Models.CheckoutStatus.InProgress
            }
        };

        await _repository.Insert(entity, cancellationToken);

        return Result.Ok(entity.Id);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(id, cancellationToken);

        return affectedRows > 0 ? Result.Ok() : Result.Fail(Domain.Errors.Checkout.SaveFailed);
    }

    public async Task<Result<Domain.Models.Checkout>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.Checkout[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventIdAndUserId(Guid eventId, Guid identityId, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        entities = [.. entities.Where(e => e.IdentityId == identityId)];

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

        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByIdentityId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.IdentityId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<bool> HasArticle(Guid eventId, Guid articleId, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        if (entities.Length == 0)
        {
            return false;
        }

        var ids = entities.SelectMany(e => e.Json.ArticleIds).ToHashSet();
        return ids.Contains(articleId);
    }

    public async Task<Result> Update(Domain.Models.Checkout model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Ok() : Result.Fail(Domain.Errors.Checkout.SaveFailed);
    }
}
