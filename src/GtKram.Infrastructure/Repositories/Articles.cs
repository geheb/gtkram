using ErrorOr;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Articles : IArticles
{
    private readonly TableLocker _tableLocker;
    private readonly ISqlRepository<Article> _repository;

    public Articles(
        TableLocker tableLocker,
        ISqlRepository<Article> repository)
    {
        _tableLocker = tableLocker;
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        using var locker = await _tableLocker.LockLabelNumber(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.SellerArticle.SaveFailed;
        }

        var max = await _repository.MaxBy(e => e.LabelNumber, e => e.SellerId, model.SellerId, cancellationToken);

        var entity = model.MapToEntity(new() { Json = new() });
        entity.Json.LabelNumber = ++max;

        await _repository.Insert(entity, cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.Article[] models, Guid sellerId, CancellationToken cancellationToken)
    {
        if (models.Length == 0)
        {
            return Domain.Errors.SellerArticle.Empty;
        }

        using var locker = await _tableLocker.LockLabelNumber(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.SellerArticle.SaveFailed;
        }

        try
        {
            await using var trans = await _repository.CreateTransaction(cancellationToken);

            var max = await _repository.MaxBy(e => e.LabelNumber, e => e.SellerId, sellerId, cancellationToken);

            foreach (var model in models)
            {
                var entity = model.MapToEntity(new() { Json = new() });
                entity.Json.SellerId = sellerId;
                entity.Json.LabelNumber = ++max;

                await _repository.Insert(entity, cancellationToken);
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
        var result = await _repository.Delete(id, cancellationToken);
        return result > 0 ? Result.Success : Domain.Errors.SellerArticle.DeleteFailed;
    }

    public async Task<ErrorOr<Domain.Models.Article>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<ErrorOr<Domain.Models.Article>> FindBySellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.SellerId, sellerId, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.LabelNumber == labelNumber);
        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.SellerId, id, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.Article>(ids.Length);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repository.SelectBy(0, e => e.SellerId, chunk, cancellationToken);
            result.AddRange(entities.Select(e => e.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<Domain.Models.Article[]> GetById(ICollection<Guid> ids, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<ErrorOr<int>> GetCountBySellerId(Guid id, CancellationToken cancellationToken)
    {
        using var locker = await _tableLocker.LockLabelNumber(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.SellerArticle.Timeout;
        }

        var count = await _repository.CountBy(e => e.SellerId, id, cancellationToken);
        return  count;
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.SellerArticle.SaveFailed;
    }
}
