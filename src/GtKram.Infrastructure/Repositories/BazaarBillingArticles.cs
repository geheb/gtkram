using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarBillingArticles : IBazaarBillingArticles
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public BazaarBillingArticles(
       AppDbContext dbContext,
       ILogger<BazaarBillingArticles> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BazaarBillingArticleDto[]> GetAll(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBillingArticle = _dbContext.Set<BazaarBillingArticle>();

        var entities = await dbSetBazaarBillingArticle
            .AsNoTracking()
            .Include(e => e.BazaarSellerArticle)
            .ThenInclude(e => e!.BazaarSeller)
            .Where(e => e.BazaarBillingId == billingId && e.BazaarBilling!.BazaarEventId == eventId)
            .OrderByDescending(e => e.CreatedOn)
            .Select(e => new { e.Id, e.BazaarSellerArticleId, e.CreatedOn, e.BazaarSellerArticle!.BazaarSeller!.SellerNumber, e.BazaarSellerArticle.Name, e.BazaarSellerArticle.LabelNumber, e.BazaarSellerArticle.Price })
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => new BazaarBillingArticleDto
        {
            Id = e.Id,
            ArticleId = e.BazaarSellerArticleId!.Value,
            AddedOn = dc.ToLocal(e.CreatedOn),
            SellerNumber = e.SellerNumber,
            Name = e.Name,
            LabelNumber = e.LabelNumber,
            Price = e.Price
        }).ToArray();
    }

    public async Task<(BazaarArticleStatus status, Guid? billingArticleId)> Create(Guid eventId, Guid billingId, int sellerNumber, int labelNumber, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var article = await dbSetBazaarSellerArticle
            .FirstOrDefaultAsync(e => e.BazaarSeller!.BazaarEventId == eventId && e.BazaarSeller.SellerNumber == sellerNumber && e.LabelNumber == labelNumber, cancellationToken);

        if (article == null)
        {
            var hasSeller = await dbSetBazaarSellerArticle
                .AsNoTracking()
                .AnyAsync(e => e.BazaarSeller!.BazaarEventId == eventId && e.BazaarSeller.SellerNumber == sellerNumber, cancellationToken);

            if (!hasSeller)
            {
                _logger.LogWarning("Seller {SellerNumber} for event {EventId} not found.", sellerNumber, eventId);
                return (BazaarArticleStatus.SellerNotFound, default);
            }

            _logger.LogWarning("Article {LabelNumber} with seller {SellerNumber} for event {EventId} not found.", labelNumber, sellerNumber, eventId);
            return (BazaarArticleStatus.ArticelNotFound, default);
        }

        return await Create(article, billingId, cancellationToken);
    }

    public async Task<(BazaarArticleStatus status, Guid? billingArticleId)> Create(Guid eventId, Guid billingId, Guid articleId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var article = await dbSetBazaarSellerArticle
            .FirstOrDefaultAsync(e => e.Id == articleId && e.BazaarSeller!.BazaarEventId == eventId, cancellationToken);

        if (article == null)
        {
            _logger.LogWarning("Article {ArticleId} for event {EventId} not found.", articleId, eventId);
            return (BazaarArticleStatus.ArticelNotFound, default);
        }

        return await Create(article, billingId, cancellationToken);
    }

    public async Task<BazaarBillingArticleDto?> Find(Guid id, CancellationToken cancellationToken)
    {
        var dbSetBazaarBillingArticle = _dbContext.Set<BazaarBillingArticle>();

        var entity = await dbSetBazaarBillingArticle
            .AsNoTracking()
            .Include(e => e.BazaarSellerArticle)
            .ThenInclude(e => e!.BazaarSeller)
            .Where(e => e.Id == id)
            .Select(e => new { e.Id, e.BazaarSellerArticleId, e.CreatedOn, e.BazaarSellerArticle!.BazaarSeller!.SellerNumber, e.BazaarSellerArticle.Name, e.BazaarSellerArticle.LabelNumber, e.BazaarSellerArticle.Price })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return default;
        }

        var dc = new GermanDateTimeConverter();

        return new BazaarBillingArticleDto
        {
            Id = entity.Id,
            ArticleId = entity.BazaarSellerArticleId!.Value,
            AddedOn = dc.ToLocal(entity.CreatedOn),
            SellerNumber = entity.SellerNumber,
            Name = entity.Name,
            LabelNumber = entity.LabelNumber,
            Price = entity.Price
        };
    }

    public async Task<bool> Delete(Guid eventId, Guid billingId, Guid id, CancellationToken cancellationToken)
    {
        var dbSetBazaarBillingArticle = _dbContext.Set<BazaarBillingArticle>();

        var entity = await dbSetBazaarBillingArticle
            .Include(e => e.BazaarBilling)
            .Include(e => e.BazaarSellerArticle)
            .FirstOrDefaultAsync(e => e.Id == id & e.BazaarBillingId == billingId && e.BazaarBilling!.BazaarEventId == eventId, cancellationToken);

        if (entity == null) return false;

        using var trans = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        entity.BazaarSellerArticle!.Status = (int)Domain.Models.SellerArticleStatus.Created;
        entity.BazaarBilling!.Status = (int)Domain.Models.BillingStatus.InProgress; // reset if already completed
        entity.BazaarBilling.Total -= entity.BazaarSellerArticle.Price;

        dbSetBazaarBillingArticle.Remove(entity);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return false;
        }

        await trans.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> Cancel(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var billing = await dbSetBazaarBilling
            .Include(e => e.BazaarBillingArticles!)
            .ThenInclude(e => e.BazaarSellerArticle)
            .FirstOrDefaultAsync(e => e.Id == billingId && e.BazaarEventId == eventId, cancellationToken);

        if (billing == null) return false;

        using var trans = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (billing.BazaarBillingArticles!.Any())
        {
            foreach (var article in billing.BazaarBillingArticles!)
            {
                article.BazaarSellerArticle!.Status = (int)Domain.Models.SellerArticleStatus.Created;
            }

            if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
            {
                return false;
            }

            var dbSetBazaarBillingArticle = _dbContext.Set<BazaarBillingArticle>();

            dbSetBazaarBillingArticle.RemoveRange(billing.BazaarBillingArticles);

            if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
            {
                return false;
            }
        }

        dbSetBazaarBilling.Remove(billing);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return false;
        }

        await trans.CommitAsync(cancellationToken);

        return true;
    }

    private async Task<(BazaarArticleStatus status, Guid? billingArticleId)> Create(BazaarSellerArticle article, Guid billingId, CancellationToken cancellationToken)
    {
        var dbSetBazaarBillingArticle = _dbContext.Set<BazaarBillingArticle>();

        var billingArticle = await dbSetBazaarBillingArticle
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BazaarSellerArticleId == article.Id, cancellationToken);

        if (billingArticle != null)
        {
            _logger.LogWarning("BazaarBillingArticle {Id} exists already.", billingArticle.Id);
            return (BazaarArticleStatus.Exists, billingArticle.Id);
        }

        billingArticle = new BazaarBillingArticle
        {
            Id = _pkGenerator.Generate(),
            CreatedOn = DateTimeOffset.UtcNow,
            BazaarBillingId = billingId,
            BazaarSellerArticleId = article.Id
        };

        using var trans = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        article.Status = (int)Domain.Models.SellerArticleStatus.Booked;

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return (BazaarArticleStatus.SaveFailed, default);
        }

        var dbSetBazaarBilling = _dbContext.Set<BazaarBilling>();

        var billing = await dbSetBazaarBilling.FindAsync(new object[] { billingId }, cancellationToken);
        if (billing == null)
        {
            _logger.LogWarning("Billing {Id} not found.", billingId);
            return (BazaarArticleStatus.SaveFailed, default);
        }

        await dbSetBazaarBillingArticle.AddAsync(billingArticle, cancellationToken);

        billing.Status = (int)Domain.Models.BillingStatus.InProgress; // reset if completed
        billing.Total += article.Price;

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return (BazaarArticleStatus.SaveFailed, default);
        }

        await trans.CommitAsync(cancellationToken);

        return (BazaarArticleStatus.Created, billingArticle.Id);
    }
}
