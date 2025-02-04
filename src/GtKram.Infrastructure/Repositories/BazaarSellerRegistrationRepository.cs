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
    private static readonly SemaphoreSlim _registerSemaphore = new SemaphoreSlim(1, 1);

    private const string _notFound = "Die Registrierung wurde nicht gefunden.";
    private const string _saveFailed = "Die Registrierung konnte nicht gespeichert werden.";
    private const string _notProcessed = "Die Registrierung konnte leider nicht bearbeitet werden. Bitte erneut versuchen.";

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
        if (!await _registerSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(_notProcessed);
        }

        try
        {
            var entity = model.MapToEntity(new(), new());
            entity.Id = _pkGenerator.Generate();
            entity.CreatedOn = _timeProvider.GetUtcNow();

            await _dbSet.AddAsync(entity, cancellationToken);

            var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            return isAdded ? Result.Ok() : Result.Fail(_saveFailed);
        }
        finally
        {
            _registerSemaphore.Release();
        }
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

    public async Task<Result<BazaarSellerRegistration>> FindByBazaarSellerId(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.BazaarSellerId == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<Result<BazaarSellerRegistration>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<BazaarSellerRegistration[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _dbSet.ToArrayAsync(cancellationToken);
        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
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

    public async Task<Result<int>> GetCountByBazaarEventId(Guid id, CancellationToken cancellationToken)
    {
        if (!await _registerSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(_notProcessed);
        }

        try
        {
            return await _dbSet.CountAsync(e => e.BazaarEventId == id, cancellationToken);
        }
        finally
        {
            _registerSemaphore.Release();
        }
    }
}
