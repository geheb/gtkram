using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarBillings : IBazaarBillings
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly IUsers _users;
    private readonly ILogger _logger;

    public BazaarBillings(
       AppDbContext dbContext,
       IUsers users,
       ILogger<BazaarBillings> logger)
    {
        _dbContext = dbContext;
        _users = users;
        _logger = logger;
    }

    public async Task<BazaarBillingDto[]> GetAll(Guid eventId, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();
        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var entities = await dbSetBazaarBilling
            .AsNoTracking()
            .Include(e => e.User)
            .Select(e => new { billing = e, count = e.BazaarBillingArticles!.Count })
            .Where(e => e.billing.BazaarEventId == eventId)
            .OrderByDescending(e => e.billing.CreatedOn)
            .ToArrayAsync(cancellationToken);

        return entities.Select(e => e.billing.MapToDto(e.count, dc)).ToArray();
    }

    public async Task<BazaarBillingDto[]> GetAll(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var entities = await dbSetBazaarBilling
           .AsNoTracking()
           .Include(e => e.User)
           .Select(e => new { billing = e, count = e.BazaarBillingArticles!.Count })
           .Where(e => e.billing.BazaarEventId == eventId && e.billing.UserId == userId)
           .OrderByDescending(e => e.billing.CreatedOn)
           .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => e.billing.MapToDto(e.count, dc)).ToArray();
    }

    public async Task<Guid> Create(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var entity = new BazaarBilling
        {
            Id = _pkGenerator.Generate(),
            BazaarEventId = eventId,
            Status = (int)BillingStatus.InProgress,
            UserId = userId
        };

        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        await dbSetBazaarBilling.AddAsync(entity, cancellationToken);

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? entity.Id : Guid.Empty;
    }

    public async Task<BazaarBillingDto?> Find(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var entity = await dbSetBazaarBilling
           .AsNoTracking()
           .Include(e => e.User)
           .Select(e => new { billing = e, count = e.BazaarBillingArticles!.Count })
           .FirstOrDefaultAsync(e => e.billing.Id == billingId && e.billing.BazaarEventId == eventId, cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entity != null ? entity.billing.MapToDto(entity.count, dc) : null;
    }

    public async Task<bool> SetAsCompleted(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var billing = await dbSetBazaarBilling
            .Include(e => e.BazaarBillingArticles!)
            .ThenInclude(e => e.BazaarSellerArticle)
            .FirstOrDefaultAsync(e => e.Id == billingId && e.BazaarEventId == eventId, cancellationToken);

        if (billing == null || billing.Total < 1) return false;

        using var trans = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        billing.Status = (int)BillingStatus.Completed;

        foreach (var article in billing.BazaarBillingArticles!)
        {
            article.BazaarSellerArticle!.Status = (int)SellerArticleStatus.Sold;
        }

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return false;
        }

        await trans.CommitAsync(cancellationToken);

        return true;
    }
}
