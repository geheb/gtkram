using FluentResults;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarSellerRepository : IBazaarSellerRepository
{
    private const string _notFound = "Der Verkäufer wurde nicht gefunden.";
    private const string _saveFailed = "Der Verkäufer konnte nicht gespeichert werden.";
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

    public async Task<Result> Create(BazaarSeller model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok() : Result.Fail(_saveFailed);
    }

    public async Task<Result<BazaarSeller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain();
    }

    public async Task<BazaarSeller[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .Where(e => e.BazaarEventId == id)
            .ToArrayAsync(cancellationToken);

        return entities.Select(e => e.MapToDomain()).ToArray();
    }

    public async Task<Result> Update(BazaarSeller model, CancellationToken cancellationToken)
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
