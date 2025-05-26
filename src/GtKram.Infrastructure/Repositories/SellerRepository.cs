using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories.Mappings;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRepository : ISellerRepository
{
    private static readonly SemaphoreSlim _sellerNumberSemaphore = new SemaphoreSlim(1, 1);

    private readonly IRepository<Persistence.Entities.Seller> _repo;

    public SellerRepository(IRepository<Persistence.Entities.Seller> repo)
    {
        _repo = repo;
    }

    public async Task<Result<Guid>> Create(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new());
        entity.UserId = model.UserId;

        if (!await _sellerNumberSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.Seller.SaveFailed);
        }

        try
        {
            await using var trans = await _repo.BeginTransaction(cancellationToken);

            if (entity.SellerNumber < 1)
            {
                var maxSellerNumber = await _repo.Max<int>(
                    e => e.SellerNumber,
                    [new(static e => e.EventId, model.EventId)],
                    trans,
                    cancellationToken);

                entity.SellerNumber = (maxSellerNumber ?? 0) + 1;
            }
            else
            {
                var entities = await _repo.Query(
                    [new(static e => e.EventId, model.EventId)],
                    trans,
                    cancellationToken);

                var updates = new List<Persistence.Entities.Seller>();

                var max = entities.Max(e => e.Item.SellerNumber);
                foreach (var e in entities.Where(e => e.Item.SellerNumber == entity.SellerNumber))
                {
                    e.Item.SellerNumber = ++max;
                    updates.Add(e.Item);
                }

                if (updates.Count > 0)
                {
                    var result = await _repo.Update([.. updates], trans, cancellationToken);
                    if (result != UpdateResult.Success)
                    {
                        return Result.Fail(Domain.Errors.Seller.SaveFailed);
                    }
                }
            }

            await _repo.Create(entity, trans, cancellationToken);

            await trans.CommitAsync(cancellationToken);

            return Result.Ok(entity.Id);
        }
        finally
        {
            _sellerNumberSemaphore.Release();
        }
    }

    public async Task<Result<Domain.Models.Seller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, null, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return entity.Value.MapToDomain(new());
    }

    public async Task<Domain.Models.Seller[]> GetByEventId(Guid id, CancellationToken cancellationToken)
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
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Domain.Models.Seller[]> GetByUserId(Guid id, CancellationToken cancellationToken)
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
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Result<Domain.Models.Seller>> GetByUserIdAndEventId(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.UserId, userId),
                new(static e => e.EventId, eventId)
            ],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return Result.Ok(entities[0].MapToDomain(new()));
    }

    public async Task<Result> Update(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entityItem = await _repo.Find(model.Id, null, cancellationToken);
        if (entityItem is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        var entity = entityItem.Value.Item;
        model.MapToEntity(entity);


        if (!await _sellerNumberSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.Seller.SaveFailed);
        }

        try
        {
            await using var trans = await _repo.BeginTransaction(cancellationToken);

            var entities = await _repo.Query(
                [new(static e => e.EventId, entity.EventId!.Value)],
                trans,
                cancellationToken);

            var updates = new List<Persistence.Entities.Seller>
            {
                entity
            };

            var max = entities.Where(e => e.Id != model.Id).Max(e => e.Item.SellerNumber);
            foreach (var e in entities.Where(e => e.Id != entity.Id && e.Item.SellerNumber == entity.SellerNumber))
            {
                e.Item.SellerNumber = ++max;
                updates.Add(e.Item);
            }

            if (updates.Count > 0)
            {
                var result = await _repo.Update([.. updates], trans, cancellationToken);
                if (result != UpdateResult.Success)
                {
                    return Result.Fail(Domain.Errors.Seller.SaveFailed);
                }
            }

            await trans.CommitAsync(cancellationToken);

            return Result.Ok();
        }
        finally
        {
            _sellerNumberSemaphore.Release();
        }
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repo.Delete(id, null, cancellationToken);

        return affectedRows > 0 ? Result.Ok() : Result.Fail(Domain.Errors.Seller.SaveFailed);
    }

    public async Task<Domain.Models.Seller[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.Get(null, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Seller[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repo.Get(
            ids,
            null,
            cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Result<Domain.Models.Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.EventId, eventId),
                new(static e => e.SellerNumber, sellerNumber)
            ],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        return Result.Ok(entities[0].MapToDomain(new()));
    }
}
