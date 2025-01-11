using GtKram.Core.Database;
using GtKram.Core.Entities;
using GtKram.Core.Models.Bazaar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GtKram.Core.Repositories;

public sealed class BazaarSellerArticles
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public BazaarSellerArticles(AppDbContext dbContext, ILogger<BazaarSellerArticles> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BazaarSellerArticleDto[]> GetAll(Guid bazaarSellerId, Guid userId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var entities = await dbSetBazaarSellerArticle
            .AsNoTracking()
            .Include(e => e.BazaarSeller)
            .Where(e => e.BazaarSellerId == bazaarSellerId && e.BazaarSeller!.UserId == userId)
            .ToArrayAsync(cancellationToken);

        return entities.OrderBy(e => e.LabelNumber).Select(e => new BazaarSellerArticleDto(e)).ToArray();
    }

    public async Task<bool> Create(Guid bazaarSellerId, Guid userId, BazaarSellerArticleDto article, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var bazaarSeller = await dbSetBazaarSeller.FindAsync(new object[] { bazaarSellerId }, cancellationToken);
        if (bazaarSeller == null || bazaarSeller.UserId != userId)
        {
            _logger.LogError("BazaarSeller {Id} for user {UserId} not found.", bazaarSellerId, userId);
            return false;
        }

        var entity = new BazaarSellerArticle();
        if (!article.To(entity))
        {
            _logger.LogError("Create BazaarSellerArticle for user {UserId} failed.", userId);
            return false;
        }

        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var maxLabel = await dbSetBazaarSellerArticle
            .Where(e => e.BazaarSellerId == bazaarSellerId)
            .MaxAsync(e => (int?)e.LabelNumber, cancellationToken) ?? 0;

        entity.Id = _pkGenerator.Generate();
        entity.Status = (int)SellerArticleStatus.Created;
        entity.BazaarSellerId = bazaarSellerId;
        entity.LabelNumber = maxLabel + 1;

        await dbSetBazaarSellerArticle.AddAsync(entity, cancellationToken);

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<BazaarSellerArticleDto?> Find(Guid sellerId, int labelNumber, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var entity = await dbSetBazaarSellerArticle
           .AsNoTracking()
           .Include(e => e.BazaarSeller)
           .FirstOrDefaultAsync(e => e.BazaarSellerId == sellerId && e.LabelNumber == labelNumber, cancellationToken);

        return entity != null ? new BazaarSellerArticleDto(entity) : null;
    }

    public async Task<BazaarSellerArticleDto?> Find(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        var entity = await dbSetBazaarSellerArticle
           .AsNoTracking()
           .Include(e => e.BazaarSeller)
           .FirstOrDefaultAsync(e => e.Id == id && e.BazaarSeller!.UserId == userId, cancellationToken);

        return entity != null ? new BazaarSellerArticleDto(entity) : null;
    }

    public async Task<bool> Delete(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = await dbSetBazaarSellerArticle
           .Include(e => e.BazaarSeller)
           .FirstOrDefaultAsync(e => e.Id == id && e.BazaarSeller!.UserId == userId, cancellationToken);

        if (entity == null)
        {
            _logger.LogError("Find BazaarSellerArticle {Id} for user {UserId} failed.", id, userId);
            return false;
        }

        var bazaarSellerId = entity.BazaarSellerId;

        dbSetBazaarSellerArticle.Remove(entity);

        var entities = await dbSetBazaarSellerArticle
            .Where(e => e.BazaarSellerId == bazaarSellerId)
            .ToArrayAsync(cancellationToken);

        if (entities.Length > 0)
        {
            int index = 1;
            foreach (var e in entities.OrderBy(e => e.LabelNumber))
            {
                if (e.Id == entity.Id) continue;

                if (e.LabelNumber != index)
                {
                    e.LabelNumber = index;
                }
                index++;
            }
        }

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1) return false;

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TakeOverArticles(Guid bazaarSellerId, Guid userId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();
        var now = DateTimeOffset.UtcNow;

        var currentSeller = await dbSetBazaarSeller
            .AsNoTracking()
            .Where(e => e.Id == bazaarSellerId && e.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentSeller == null) return false;

        var maxArticleCount = currentSeller.MaxArticleCount;
        var dbSetBazaarEvents = _dbContext.Set<BazaarEvent>();

        var lastEvent = await dbSetBazaarEvents.OrderByDescending(e => e.StartDate).Skip(1).FirstOrDefaultAsync(cancellationToken);
        if (lastEvent is null) return false;

        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();
        var status = (int)SellerArticleStatus.Created;
        var oldArticles = await dbSetBazaarSellerArticle
            .AsNoTracking()
            .Where(e => e.BazaarSellerId != bazaarSellerId && e.BazaarSeller!.BazaarEventId == lastEvent.Id && e.BazaarSeller.UserId == userId && e.Status == status)
            .ToArrayAsync(cancellationToken);

        if (oldArticles.Length < 1) return false;

        var currentArticles = await dbSetBazaarSellerArticle
            .AsNoTracking()
            .Where(e => e.BazaarSellerId == bazaarSellerId && e.BazaarSeller!.UserId == userId)
            .ToDictionaryAsync(e => e.Name + e.Size, cancellationToken);

        if (currentArticles.Count == maxArticleCount) return false;

        var maxLabelNumber = currentArticles.Count < 1 ? 0 : currentArticles.Values.Max(e => e.LabelNumber);
        int count = 0;

        foreach (var old in oldArticles)
        {
            if (currentArticles.ContainsKey(old.Name + old.Size)) continue;

            var newArticle = new BazaarSellerArticle
            {
                Id = _pkGenerator.Generate(),
                BazaarSellerId = bazaarSellerId,
                Name = old.Name,
                Price = old.Price,
                Size = old.Size,
                Status = (int)SellerArticleStatus.Created,
                LabelNumber = ++maxLabelNumber
            };

            await dbSetBazaarSellerArticle.AddAsync(newArticle, cancellationToken);
            count++;

            if (currentArticles.Count + count == maxArticleCount) break;
        }

        if (count < 1) return false;

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1) return default;

        return true;
    }

    public async Task<bool> Update(Guid bazaarSellerId, Guid userId, BazaarSellerArticleDto article, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var bazaarSeller = await dbSetBazaarSeller.FindAsync(new object[] { bazaarSellerId }, cancellationToken);
        if (bazaarSeller == null || bazaarSeller.UserId != userId)
        {
            _logger.LogError("BazaarSeller {Id} for user {UserId} not found.", bazaarSellerId, userId);
            return false;
        }

        var dbSetBazaarSellerArticle = _dbContext.Set<BazaarSellerArticle>();
        var entity = await dbSetBazaarSellerArticle.FindAsync(new object[] { article.Id! }, cancellationToken);
        if (entity?.BazaarSellerId != bazaarSellerId)
        {
            _logger.LogError("BazaarSellerArticle {Id} not found.", article.Id);
            return false;
        }

        if (entity.Status != (int)SellerArticleStatus.Created)
        {
            _logger.LogError("BazaarSellerArticle {Id} has wrong status.", article.Id);
            return false;
        }

        if (!article.To(entity)) return true;

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
