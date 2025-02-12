using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarSellerRepository : IBazaarSellerRepository
{
    private static readonly SemaphoreSlim _sellerNumberSemaphore = new SemaphoreSlim(1, 1);

    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarSeller> _dbSet;

    public BazaarSellerRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarSeller>();
    }

    public async Task<Result<Guid>> Create(BazaarSeller model, Guid userId, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();
        entity.UserId = userId;

        await _sellerNumberSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (entity.SellerNumber < 1)
            {
                var max = await _dbSet
                    .Where(e => e.BazaarEventId == model.BazaarEventId)
                    .MaxAsync(e => e.SellerNumber, cancellationToken);

                entity.SellerNumber = max + 1;
            }
            else
            {
                var entities = await _dbSet
                    .Where(e => e.BazaarEventId == model.BazaarEventId)
                    .ToArrayAsync(cancellationToken);

                var max = entities.Max(e => e.SellerNumber);
                foreach (var e in entities.Where(e => e.SellerNumber == entity.SellerNumber))
                {
                    e.SellerNumber = ++max;
                }
            }
        }
        finally
        {
            _sellerNumberSemaphore.Release();
        }

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok(entity.Id) : Result.Fail(Seller.SaveFailed);
    }

    public async Task<Result<BazaarSeller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Seller.NotFound);
        }

        return entity.MapToDomain();
    }

    public async Task<Result<BazaarSeller>> Find(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Seller.NotFound);
        }

        return entity.MapToDomain();
    }

    public async Task<BazaarSeller[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.BazaarEventId == id)
            .ToArrayAsync(cancellationToken);

        return entities.Select(e => e.MapToDomain()).ToArray();
    }

    public async Task<BazaarSeller[]> GetByUserId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.UserId == id)
            .ToArrayAsync(cancellationToken);

        return entities.Select(e => e.MapToDomain()).ToArray();
    }

    public async Task<Result> Update(BazaarSeller model, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Seller.NotFound);
        }

        model.MapToEntity(entity);
        entity.UpdatedOn = _timeProvider.GetUtcNow();

        await _sellerNumberSemaphore.WaitAsync(cancellationToken);
        try
        {
            var entities = await _dbSet
                .Where(e => e.BazaarEventId == entity.BazaarEventId)
                .ToArrayAsync(cancellationToken);

            var max = entities.Max(e => e.SellerNumber);

            foreach (var e in entities.Where(e => e.Id != entity.Id && e.SellerNumber == entity.SellerNumber))
            {
                e.SellerNumber = ++max;
            }
        }
        finally
        {
            _sellerNumberSemaphore.Release();
        }

        var isUpdated = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isUpdated ? Result.Ok() : Result.Fail(Seller.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        _dbSet.Remove(new Persistence.Entities.BazaarSeller { Id = id });

        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(Seller.NotFound);
    }
}
