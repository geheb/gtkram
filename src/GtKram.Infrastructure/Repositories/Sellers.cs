using ErrorOr;
using GtKram.Application.Converter;
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

    public async Task<ErrorOr<Guid>> Create(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new() { Json = new() });
        entity.Json.IdentityId = model.IdentityId;

        using var locker = await _tableLocker.LockSellerNumber(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.Seller.SaveFailed;
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
                        return Domain.Errors.Seller.SaveFailed;
                    }
                }
            }

            await _repository.Insert(entity, cancellationToken);

            await trans.CommitAsync(cancellationToken);

            return entity.Id;
        }
        finally
        {
            _repository.Transaction = null;
        }
    }

    public async Task<ErrorOr<Domain.Models.Seller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
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

    public async Task<ErrorOr<Domain.Models.Seller>> FindByIdentityIdAndEventId(Guid identityId, Guid eventId, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.IdentityId, identityId, cancellationToken);
        entities = [.. entities.Where(e => e.EventId == eventId)];
        if (entities.Length == 0)
        {
            return Domain.Errors.Seller.NotFound;
        }

        return entities[0].MapToDomain(new());
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        model.MapToEntity(entity);

        /*using var locker = await _tableLocker.LockSellerNumber(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.Seller.SaveFailed;
        }*/

        try
        {
            await using var trans = await _repository.CreateTransaction(cancellationToken);

            var entities = await _repository.SelectBy(0, e => e.EventId, entity.EventId, cancellationToken);

            var updates = new List<Seller>
            {
                entity
            };

            var max = entities.Max(e => e.Id != model.Id ? e.SellerNumber : 0);

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
                    return Domain.Errors.Seller.SaveFailed;
                }
            }

            await trans.CommitAsync(cancellationToken);

            return Result.Success;
        }
        finally
        {
            _repository.Transaction = null;
        }
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(id, cancellationToken);

        return affectedRows > 0 ? Result.Success : Domain.Errors.Seller.SaveFailed;
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

    public async Task<ErrorOr<Domain.Models.Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.SellerNumber == sellerNumber);

        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        return entity.MapToDomain(new());
    }
}
