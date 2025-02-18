using GtKram.Domain.Base;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;
using GtKram.Domain.Errors;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarBillingArticleRepository : IBazaarBillingArticleRepository
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarBillingArticle> _dbSet;

    public BazaarBillingArticleRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarBillingArticle>();
    }

    public async Task<Result<Guid>> Create(Guid billingId, Guid sellerArticleId, CancellationToken cancellationToken)
    {
        var entity = new Persistence.Entities.BazaarBillingArticle
        {
            Id = _pkGenerator.Generate(),
            CreatedOn = _timeProvider.GetUtcNow(),
            BazaarBillingId = billingId,
            BazaarSellerArticleId = sellerArticleId
        };

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok(entity.Id) : Result.Fail(BillingArticle.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        _dbSet.Remove(new Persistence.Entities.BazaarBillingArticle { Id = id });

        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(BillingArticle.NotFound);
    }

    public async Task<Result> DeleteByBillingId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.BazaarBillingId == id)
            .ToArrayAsync(cancellationToken);

        _dbSet.RemoveRange(entities);
        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(BillingArticle.DeleteFailed);
    }

    public async Task<Result<BazaarBillingArticle>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(BillingArticle.NotFound);
        }

        return Result.Ok(entity.MapToDomain(new()));
    }

    public async Task<Result<BazaarBillingArticle>> FindBySellerArticleId(Guid sellerArticleId, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BazaarSellerArticleId == sellerArticleId, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(BillingArticle.NotFound);
        }

        return Result.Ok(entity.MapToDomain(new()));
    }

    public async Task<BazaarBillingArticle[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<BazaarBillingArticle[]> GetByBazaarBillingId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.BazaarBillingId == id)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<BazaarBillingArticle[]> GetByBazaarBillingId(Guid[] ids, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();
        var result = new List<BazaarBillingArticle>(ids.Length);
        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _dbSet
                .AsNoTracking()
                .Where(e => chunk.Contains(e.BazaarBillingId!.Value))
                .ToArrayAsync(cancellationToken);

            result.AddRange(entities.Select(e => e.MapToDomain(dc)));
        }

        return result.ToArray();
    }
}
