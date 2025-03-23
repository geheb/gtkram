using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarSellerArticleRepository : IBazaarSellerArticleRepository
{
    private static readonly SemaphoreSlim _labelSemaphore = new SemaphoreSlim(1, 1);

    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarSellerArticle> _dbSet;

    public BazaarSellerArticleRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarSellerArticle>();
    }

    public async Task<Result> Create(BazaarSellerArticle model, CancellationToken cancellationToken)
    {
        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(SellerArticle.SaveFailed);
        }

        try
        {
            var maxLabelNumber = await _dbSet
                .Where(e => e.BazaarSellerId == model.BazaarSellerId)
                .Select(e => (int?)e.LabelNumber)
                .MaxAsync(cancellationToken) ?? 0;

            var entity = model.MapToEntity(new());
            entity.Id = _pkGenerator.Generate();
            entity.CreatedOn = _timeProvider.GetUtcNow();
            entity.LabelNumber = maxLabelNumber + 1;

            await _dbSet.AddAsync(entity, cancellationToken);

            var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            return isAdded ? Result.Ok() : Result.Fail(SellerArticle.SaveFailed);
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Create(BazaarSellerArticle[] models, Guid sellerId, CancellationToken cancellationToken)
    {
        if (models.Length == 0)
        {
            return Result.Fail(SellerArticle.Empty);
        }

        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(SellerArticle.SaveFailed);
        }

        try
        {
            var maxLabelNumber = await _dbSet
                .Where(e => e.BazaarSellerId == sellerId)
                .Select(e => (int?)e.LabelNumber)
                .MaxAsync(cancellationToken) ?? 0;

            foreach (var model in models)
            {
                var entity = model.MapToEntity(new());
                entity.Id = _pkGenerator.Generate();
                entity.CreatedOn = _timeProvider.GetUtcNow();
                entity.BazaarSellerId = sellerId;
                entity.LabelNumber = ++maxLabelNumber;

                await _dbSet.AddAsync(entity, cancellationToken);
            }

            var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            return isAdded ? Result.Ok() : Result.Fail(SellerArticle.MultipleSaveFailed);
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(SellerArticle.NotFound);
        }

        _dbSet.Remove(entity);

        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(SellerArticle.SaveFailed);
    }

    public async Task<Result<BazaarSellerArticle>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(SellerArticle.NotFound);
        }

        return entity.MapToDomain();
    }

    public async Task<Result<BazaarSellerArticle>> FindByBazaarSellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BazaarSellerId == sellerId && e.LabelNumber == labelNumber, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(SellerArticle.NotFound);
        }

        return entity.MapToDomain();
    }

    public async Task<BazaarSellerArticle[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
        return entities.Select(e => e.MapToDomain()).ToArray();
    }

    public async Task<BazaarSellerArticle[]> GetByBazaarSellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.BazaarSellerId == id)
            .ToArrayAsync(cancellationToken);

        return entities.Select(e => e.MapToDomain()).ToArray();
    }

    public async Task<BazaarSellerArticle[]> GetByBazaarSellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<BazaarSellerArticle>(ids.Length);
        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _dbSet
                .AsNoTracking()
                .Where(e => chunk.Contains(e.BazaarSellerId!.Value))
                .ToArrayAsync(cancellationToken);

            result.AddRange(entities.Select(e => e.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<BazaarSellerArticle[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<BazaarSellerArticle>(ids.Length);
        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _dbSet
                .AsNoTracking()
                .Where(e => chunk.Contains(e.Id))
                .ToArrayAsync(cancellationToken);

            result.AddRange(entities.Select(e => e.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<Result<int>> GetCountByBazaarSellerId(Guid id, CancellationToken cancellationToken)
    {
        if (!await _labelSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(SellerArticle.Timeout);
        }

        try
        {
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.BazaarSellerId == id)
                .CountAsync(cancellationToken);
        }
        finally
        {
            _labelSemaphore.Release();
        }
    }

    public async Task<Result> Update(BazaarSellerArticle model, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(SellerArticle.NotFound);
        }

        model.MapToEntity(entity);
        entity.UpdatedOn = _timeProvider.GetUtcNow();

        var isUpdated = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isUpdated ? Result.Ok() : Result.Fail(SellerArticle.SaveFailed);
    }
}
