using FluentResults;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarSellerRegistrationRepository : IBazaarSellerRegistrationRepository
{
    private const string _notFound = "Die Registrierung wurde nicht gefunden.";
    private const string _saveFailed = "Die Registrierung konnte nicht gespeichert werden.";
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarSellerRegistration> _dbSet;

    public BazaarSellerRegistrationRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarSellerRegistration>();
    }

    public async Task<Result> Create(BazaarSellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new(), new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok() : Result.Fail(_saveFailed);
    }

    public async Task<Result<BazaarSellerRegistration>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<BazaarSellerRegistration[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .Where(e => e.BazaarEventId == id)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Result> Update(BazaarSellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        model.MapToEntity(entity, new());
        entity.UpdatedOn = _timeProvider.GetUtcNow();

        var isUpdated = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isUpdated ? Result.Ok() : Result.Fail(_saveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        _dbSet.Remove(new Persistence.Entities.BazaarSellerRegistration { Id = id });

        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(_notFound);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountByBazaarEventId(CancellationToken cancellationToken)
    {
        var result = await _dbSet
            .GroupBy(e => e.BazaarEventId)
            .Select(g => new { Id = g.Key!.Value, Count = g.Count() })
            .ToArrayAsync(cancellationToken);

        return result.ToDictionary(r => r.Id, r => r.Count);
    }
}
