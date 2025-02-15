using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarBillingRepository : IBazaarBillingRepository
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly DbSet<Persistence.Entities.BazaarBilling> _dbSet;

    public BazaarBillingRepository(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _dbSet = _dbContext.Set<Persistence.Entities.BazaarBilling>();
    }

    public async Task<Result> Create(BazaarBilling model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new());
        entity.Id = _pkGenerator.Generate();
        entity.CreatedOn = _timeProvider.GetUtcNow();

        await _dbSet.AddAsync(entity, cancellationToken);

        var isAdded = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
        return isAdded ? Result.Ok() : Result.Fail(Domain.Errors.Billing.SaveFailed);
    }

    public async Task<BazaarBilling[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<BazaarBilling[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _dbSet
            .AsNoTracking()
            .Where(e => e.BazaarEventId == id)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }
}
