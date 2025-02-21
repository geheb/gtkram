using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarEventRepository : IBazaarEventRepository
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarEvent> _dbSet;

    public BazaarEventRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarEvent>();
    }

    public async Task<Result<Guid>> Create(BazaarEvent model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new(), new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();
        entity.Commission = 20;

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok(entity.Id) : Result.Fail(Event.SaveFailed);
    }

    public async Task<Result<BazaarEvent>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Event.NotFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<BazaarEvent[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();
        var result = new List<BazaarEvent>(ids.Length);
        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _dbSet
                .AsNoTracking()
                .Where(e => chunk.Contains(e.Id))
                .ToArrayAsync(cancellationToken);

            result.AddRange(entities.Select(e => e.MapToDomain(dc)));
        }

        return [.. result];
    }

    public async Task<BazaarEvent[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .OrderByDescending(e => e.StartDate)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Result> Update(BazaarEvent model, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Event.NotFound);
        }

        model.MapToEntity(entity, new());
        entity.UpdatedOn = _timeProvider.GetUtcNow();

        var isUpdated = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isUpdated ? Result.Ok() : Result.Fail(Event.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is null)
        {
            return Result.Fail(Event.NotFound);
        }

        _dbSet.Remove(entity);

        var isDeleted = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isDeleted ? Result.Ok() : Result.Fail(Event.DeleteFailed);
    }
}
