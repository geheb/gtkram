using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;

namespace GtKram.Infrastructure.Repositories;

internal sealed class ArticleRepository : IArticleRepository
{
    private static readonly SemaphoreSlim _labelSemaphore = new SemaphoreSlim(1, 1);
    private readonly IRepository<Persistence.Entities.Article> _repo;

    public ArticleRepository(IRepository<Persistence.Entities.Article> repo)
    {
        _repo = repo;
    }

    public async Task<Result> Create(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerArticle.SaveFailed);
        }

        try
        {
            var maxLabelNumber = await _repo.Max<int>(
                e => e.LabelNumber,
                [
                    new(static e => e.SellerId, model.SellerId)
                ],
                cancellationToken);

            var entity = model.MapToEntity(new());
            entity.LabelNumber = (maxLabelNumber ?? 0) + 1;

            await _repo.Create(entity, cancellationToken);

            return Result.Ok();
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Create(Domain.Models.Article[] models, Guid sellerId, CancellationToken cancellationToken)
    {
        if (models.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerArticle.Empty);
        }

        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerArticle.SaveFailed);
        }

        try
        {
            await using var trans = await _repo.BeginTransaction(cancellationToken);

            var maxLabelNumber = await _repo.Max<int>(
                e => e.LabelNumber,
                [
                    new(static e => e.SellerId, sellerId)
                ],
                trans,
                cancellationToken);

            var nextValue = maxLabelNumber ?? 0;

            foreach (var model in models)
            {
                var entity = model.MapToEntity(new());
                entity.SellerId = sellerId.ToChar32();
                entity.LabelNumber = ++nextValue;

                await _repo.Create(entity, trans, cancellationToken);
            }

            await trans.CommitAsync(cancellationToken);

            return Result.Ok();
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repo.Delete(id, cancellationToken);
        return result > 0 ? Result.Ok() : Result.Fail(Domain.Errors.SellerArticle.DeleteFailed);
    }

    public async Task<Result<Domain.Models.Article>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerArticle.NotFound);
        }

        return entity.Value.Item.MapToDomain();
    }

    public async Task<Result<Domain.Models.Article>> FindBySellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.SellerId, sellerId),
                new(static e => e.LabelNumber, labelNumber),
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerArticle.NotFound);
        }

        return entities[0].Item.MapToDomain();
    }

    public async Task<Domain.Models.Article[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.GetAll(cancellationToken);

        return [.. entities.Select(e => e.Item.MapToDomain())];
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.SellerId, id)
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.Item.MapToDomain())];
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.Article>(ids.Length);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repo.Query(
                [
                    new(static e => e.SellerId, chunk),
                ],
                cancellationToken);

            result.AddRange(entities.Select(e => e.Item.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<Domain.Models.Article[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repo.Get(
            ids,
            cancellationToken);

        return [.. entities.Select(e => e.Item.MapToDomain())];
    }

    public async Task<Result<int>> GetCountBySellerId(Guid id, CancellationToken cancellationToken)
    {
        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerArticle.Timeout);
        }

        try
        {
            var count = await _repo.Count(
                [
                    new(static e => e.SellerId, id)
                ],
                cancellationToken);

            return Result.Ok(count);
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Update(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerArticle.NotFound);
        }

        model.MapToEntity(entity.Value.Item);
        var result = await _repo.Update(entity.Value.Item, cancellationToken);

        return result == UpdateResult.Success ? Result.Ok() : Result.Fail(Domain.Errors.SellerArticle.SaveFailed);
    }
}
