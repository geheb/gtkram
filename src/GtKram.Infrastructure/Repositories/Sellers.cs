using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Sellers : ISellers
{
    private readonly TableLocker _tableLocker;
    private readonly ISqlRepository<Seller> _repository;

    public Sellers(
        TableLocker tableLocker,
        ISqlRepository<Seller> repository)
    {
        _tableLocker = tableLocker;
        _repository = repository;
    }

    public async Task<Result<Guid>> Create(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new() { Json = new() });
        entity.Json.IdentityId = model.IdentityId;

        if (!await _tableLocker.SellerNumber.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.Seller.SaveFailed);
        }

        try
        {
            await using var trans = await _repository.CreateTransaction(cancellationToken);

            if (entity.Json.SellerNumber == 0)
            {
                var max = await _repository.MaxBy(
                    e => e.SellerNumber, 
                    e => e.EventId, 
                    model.EventId, 
                    cancellationToken);

                entity.Json.SellerNumber = max + 1;
            }
            else
            {
                var entities = await _repository.SelectBy(
                    0,
                    e => e.EventId,
                    model.EventId,
                    cancellationToken);

                var updates = new List<Seller>();

                var max = entities.Max(e => e.SellerNumber);
                foreach (var e in entities.Where(e => e.SellerNumber == entity.SellerNumber))
                {
                    e.Json.SellerNumber = ++max;
                    updates.Add(e);
                }

                if (updates.Count > 0)
                {
                    var result = await _repository.Update(updates, cancellationToken);
                    if (result != updates.Count)
                    {
                        return Result.Fail(Domain.Errors.Seller.SaveFailed);
                    }
                }
            }

            await _repository.Insert(entity, cancellationToken);

            await trans.Commit(cancellationToken);

            return Result.Ok(entity.Id);
        }
        finally
        {
            _tableLocker.SellerNumber.Release();
        }
    }

    public async Task<Result<Domain.Models.Seller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.Seller[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Domain.Models.Seller[]> GetByIdentityId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.IdentityId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Result<Domain.Models.Seller>> FindByIdentityIdAndEventId(Guid identityId, Guid eventId, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.IdentityId, identityId, cancellationToken);
        entities = [.. entities.Where(e => e.EventId == eventId)];
        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return Result.Ok(entities[0].MapToDomain(new()));
    }

    public async Task<Result> Update(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        model.MapToEntity(entity);

        if (!await _tableLocker.SellerNumber.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.Seller.SaveFailed);
        }

        try
        {
            await using var trans = await _repository.CreateTransaction(cancellationToken);

            var entities = await _repository.SelectBy(0, e => e.EventId, entity.EventId, cancellationToken);

            var updates = new List<Seller>
            {
                entity
            };

            var max = entities
                .Where(e => e.Id != model.Id)
                .DefaultIfEmpty()
                .Max(e => e?.SellerNumber ?? 0);

            foreach (var e in entities.Where(e => e.Id != entity.Id && e.SellerNumber == entity.SellerNumber))
            {
                e.Json.SellerNumber = ++max;
                updates.Add(e);
            }

            if (updates.Count > 0)
            {
                var result = await _repository.Update(updates, cancellationToken);
                if (result != updates.Count)
                {
                    return Result.Fail(Domain.Errors.Seller.SaveFailed);
                }
            }

            await trans.Commit(cancellationToken);

            return Result.Ok();
        }
        finally
        {
            _tableLocker.SellerNumber.Release();
        }
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(id, cancellationToken);

        return affectedRows > 0 ? Result.Ok() : Result.Fail(Domain.Errors.Seller.SaveFailed);
    }

    public async Task<Domain.Models.Seller[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Seller[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Result<Domain.Models.Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        entities = [.. entities.Where(e => e.SellerNumber == sellerNumber)];

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return Result.Ok(entities[0].MapToDomain(new()));
    }
}
