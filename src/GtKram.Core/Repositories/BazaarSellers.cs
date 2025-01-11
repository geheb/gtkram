using GtKram.Core.Converter;
using GtKram.Core.Database;
using GtKram.Core.Entities;
using GtKram.Core.Models.Bazaar;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Core.Repositories;

public sealed class BazaarSellers
{
    private readonly AppDbContext _dbContext;
    private readonly Users _users;

    public BazaarSellers(AppDbContext dbContext, Users users)
    {
        _dbContext = dbContext;
        _users = users;
    }

    public async Task<BazaarSellerDto[]> GetAll(Guid userId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var entities = await dbSetBazaarSeller
            .AsNoTracking()
            .Include(e => e.BazaarEvent)
            .Include(e => e.BazaarSellerRegistration)
            .Select(e => new { seller = e, count = e.BazaarSellerArticles!.Count })
            .Where(e => e.seller.UserId == userId && e.seller.BazaarSellerRegistration!.Accepted == true)
            .OrderByDescending(e => e.seller.BazaarEvent!.StartDate)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();

        return entities.Select(e => new BazaarSellerDto(e.seller, e.count, dc)).ToArray();
    }

    public async Task<BazaarSellerDto?> Find(Guid sellerId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var entity = await dbSetBazaarSeller
            .AsNoTracking()
            .Include(e => e.BazaarEvent)
            .Include(e => e.BazaarSellerRegistration)
            .Select(e => new { seller = e, count = e.BazaarSellerArticles!.Count })
            .FirstOrDefaultAsync(e => e.seller.Id == sellerId, cancellationToken);

        if (entity == null) return null;

        var dc = new GermanDateTimeConverter();

        return new BazaarSellerDto(entity.seller, entity.count, dc);
    }

    public async Task<BazaarSellerDto?> Find(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var entity = await dbSetBazaarSeller
            .AsNoTracking()
            .Include(e => e.BazaarEvent)
            .Include(e => e.BazaarSellerRegistration)
            .Select(e => new { seller = e, count = e.BazaarSellerArticles!.Count })
            .Where(e => e.seller.BazaarEventId == eventId && e.seller.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null) return null;

        var dc = new GermanDateTimeConverter();

        return new BazaarSellerDto(entity.seller, entity.count, dc);
    }

    public static int CalcMaxArticleCount(SellerRole role) => role switch
    {
        SellerRole.Orga => 20 * 24,
        SellerRole.TeamLead => 4 * 24, // alt: 120
        SellerRole.Helper => 3 * 24, // alt: 96
        _ => 2 * 24
    };

    public async Task<bool> Update(Guid id, SellerRole role, int sellerNumber, bool canCreateBillings, CancellationToken cancellationToken)
    {
        var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

        var entity = await dbSetBazaarSeller
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null) return false;

        var hasChanges = false;
        if (entity.Role != (int)role)
        {
            hasChanges = true;
            entity.Role = (int)role;
            entity.MaxArticleCount = CalcMaxArticleCount(role);

        }
        if (entity.SellerNumber != sellerNumber)
        {
            hasChanges = true;
            entity.SellerNumber = sellerNumber;
        }
        if (entity.CanCreateBillings != canCreateBillings)
        {
            hasChanges = true;
            entity.CanCreateBillings = canCreateBillings;
        }

        if (!hasChanges) return true;

        if (canCreateBillings && entity.UserId.HasValue)
        {
            if (!await _users.AddBillingRole(entity.UserId.Value, cancellationToken))
            {
                return false;
            }
        }

        if (sellerNumber > 0)
        {
            var eventId = entity.BazaarEventId;

            var entities = await dbSetBazaarSeller
                .Where(e => e.BazaarEventId == eventId)
                .ToListAsync(cancellationToken);

            var maxSeller = entities.Max(e => e.SellerNumber);

            foreach (var e in entities.FindAll(e => e.SellerNumber == sellerNumber))
            {
                if (e.Id == id) continue;
                e.SellerNumber = ++maxSeller;
            }
        }

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
