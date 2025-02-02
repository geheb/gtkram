using FluentResults;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarSellerArticleRepository : IBazaarSellerArticleRepository
{
    private const string _notFound = "Der Artikel wurde nicht gefunden.";
    private const string _saveFailed = "Der Artikel konnte nicht gespeichert werden.";
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
        var entity = model.MapToEntity(new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok() : Result.Fail(_saveFailed);
    }

    public async Task<Result<BazaarSellerArticle>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain();
    }

    public async Task<BazaarSellerArticle[]> GetByBazaarSellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
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
                .Where(e => chunk.Contains(e.BazaarSellerId!.Value))
                .ToArrayAsync(cancellationToken);

            result.AddRange(result);
        }

        return [.. result];
    }

    public async Task<Result> Update(BazaarSellerArticle model, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        model.MapToEntity(entity);
        entity.UpdatedOn = _timeProvider.GetUtcNow();

        var isUpdated = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isUpdated ? Result.Ok() : Result.Fail(_saveFailed);
    }
}
