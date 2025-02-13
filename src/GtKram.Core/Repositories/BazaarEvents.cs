using GtKram.Core.Converter;
using GtKram.Core.Database;
using GtKram.Core.Entities;
using GtKram.Core.Models.Bazaar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GtKram.Core.Repositories;

public class BazaarEvents
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly Users _users;
    private readonly ILogger _logger;

    public BazaarEvents(
        AppDbContext dbContext,
        Users users,
        ILogger<BazaarEvents> logger)
    {
        _dbContext = dbContext;
        _users = users;
        _logger = logger;
    }
    public async Task<BazaarEventDto?> Find(Guid id, CancellationToken cancellationToken)
    {
        var billingStatus = (int)BillingStatus.Completed;
        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();

        var entity = await dbSetBazaarEvent
            .AsNoTracking()
            .Select(e => new
            {
                @event = e,
                sellerRegistrationCount = e.SellerRegistrations!.Count,
                billingCount = e.BazaarBillings!.Count,
                soldTotal = e.BazaarBillings.Where(b => b.Status == billingStatus).Sum(b => b.Total)
            })
            .OrderByDescending(e => e.@event.StartDate)
            .FirstOrDefaultAsync(e => e.@event.Id == id, cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entity != null ? new BazaarEventDto(entity.@event, entity.sellerRegistrationCount, entity.billingCount, entity.soldTotal, dc) : null;
    }

    public async Task<BazaarEventDto[]> GetAll(CancellationToken cancellationToken)
    {
        var billingStatus = (int)BillingStatus.Completed;
        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();

        var entities = await dbSetBazaarEvent
            .AsNoTracking()
            .Select(e => new
            {
                @event = e,
                sellerRegistrationCount = e.SellerRegistrations!.Count,
                billingCount = e.BazaarBillings!.Count,
                soldTotal = e.BazaarBillings.Where(b => b.Status == billingStatus).Sum(b => b.Total)
            })
            .OrderByDescending(e => e.@event.StartDate)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => new BazaarEventDto(e.@event, e.sellerRegistrationCount, e.billingCount, e.soldTotal, dc)).ToArray();
    }

    public async Task<BazaarEventDto[]> GetAll(Guid userId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();
        var userEvents = await dbSetBazaarSeller
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.CanCreateBillings)
            .Select(e => e.BazaarEventId)
            .ToArrayAsync(cancellationToken);

        if (!userEvents.Any())
        {
            return Array.Empty<BazaarEventDto>();
        }

        var billingStatus = (int)BillingStatus.Completed;
        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();

        var entities = await dbSetBazaarEvent
            .AsNoTracking()
            .Where(e => userEvents.Contains(e.Id))
            .Select(e => new
            {
                @event = e,
                sellerRegistrationCount = e.SellerRegistrations!.Count,
                billingCount = e.BazaarBillings!.Count,
                soldTotal = e.BazaarBillings.Where(b => b.Status == billingStatus).Sum(b => b.Total)
            })
            .OrderByDescending(e => e.@event.StartDate)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => new BazaarEventDto(e.@event, e.sellerRegistrationCount, e.billingCount, e.soldTotal, dc)).ToArray();
    }

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();
        dbSetBazaarEvent.Remove(new BazaarEvent { Id = id });
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Create(BazaarEventDto dto, CancellationToken cancellationToken)
    {
        var entity = new BazaarEvent();
        if (!dto.To(entity))
        {
            _logger.LogError("create BazaarEvent failed");
            return false;
        }
        entity.Id = _pkGenerator.Generate();

        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();

        await dbSetBazaarEvent.AddAsync(entity, cancellationToken);
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Update(BazaarEventDto dto, CancellationToken cancellationToken)
    {
        var dbSetBazaarEvent = _dbContext.Set<BazaarEvent>();

        var entity = await dbSetBazaarEvent.FindAsync(new object[] { dto.Id! }, cancellationToken);
        if (entity == null) return false;

        if (!dto.To(entity)) return true;

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
