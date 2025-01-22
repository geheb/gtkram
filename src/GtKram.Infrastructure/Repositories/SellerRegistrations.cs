using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrations : ISellerRegistrations, IDisposable
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly IUsers _users;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _registerSemaphore = new SemaphoreSlim(1, 1);

    public SellerRegistrations(
        AppDbContext dbContext,
        IUsers users,
        ILogger<SellerRegistrations> logger)
    {
        _dbContext = dbContext;
        _users = users;
        _logger = logger;
    }

    public void Dispose()
    {
        _registerSemaphore.Dispose();
    }

    public async Task<BazaarSellerRegistrationDto[]> GetAll(Guid eventId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        var entities = await dbSetBazaarSellerRegistration
            .AsNoTracking()
            .Include(e => e.BazaarSeller)
            .Include(e => e.BazaarEvent)
            .Where(e => e.BazaarEventId == eventId)
            .OrderBy(e => e.Name)
            .Select(e => new { reg = e, count = e.BazaarSeller!.BazaarSellerArticles!.Count })
            .ToArrayAsync(cancellationToken);

        var idn = new IdnMapping();

        return entities.Select(e => e.reg.MapToDto(e.count, idn)).ToArray();
    }

    public async Task<bool> Confirm(Guid eventId, Guid registrationId, bool confirmed, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        var entity = await dbSetBazaarSellerRegistration
            .Include(e => e.BazaarSeller)
            .Include(e => e.BazaarEvent)
            .FirstOrDefaultAsync(e => e.Id == registrationId && e.BazaarEventId == eventId, cancellationToken);

        if (entity == null) return false;

        var hasChanges = false;

        if (!confirmed)
        {
            if (entity.Accepted != confirmed)
            {
                hasChanges = true;
                entity.Accepted = false;
            }
        }
        else
        {
            if (entity.Accepted != confirmed)
            {
                hasChanges = true;
                entity.Accepted = true;
            }

            var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();

            if (!entity.BazaarSellerId.HasValue)
            {
                var maxSeller = await dbSetBazaarSeller
                    .Where(e => e.BazaarEventId == eventId)
                    .MaxAsync(e => (int?)e.SellerNumber, cancellationToken) ?? 0;

                var seller = new BazaarSeller
                {
                    Id = _pkGenerator.Generate(),
                    BazaarEventId = eventId,
                    SellerNumber = maxSeller + 1,
                    Role = (int)SellerRole.Standard,
                    MaxArticleCount = BazaarSellers.CalcMaxArticleCount(SellerRole.Standard)
                };

                await dbSetBazaarSeller.AddAsync(seller, cancellationToken);

                entity.BazaarSeller = seller;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
            {
                return false;
            }
        }

        return await NotifyRegistration(entity.Id, cancellationToken);
    }

    public async Task<bool> Delete(Guid eventId, Guid sellerId, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        var entity = await dbSetBazaarSellerRegistration
            .Include(e => e.BazaarSeller)
            .FirstOrDefaultAsync(e => e.Id == sellerId && e.BazaarEventId == eventId, cancellationToken);

        if (entity == null) return false;

        if (entity.BazaarSeller != null)
        {
            var dbSetBazaarSeller = _dbContext.Set<BazaarSeller>();
            dbSetBazaarSeller.Remove(entity.BazaarSeller);
        }

        dbSetBazaarSellerRegistration.Remove(entity);

        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<Guid?> Register(Guid eventId, BazaarSellerRegistrationDto dto, CancellationToken cancellationToken)
    {
        if (!await _registerSemaphore.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken)) return default;

        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        try
        {
            var email = new IdnMapping().GetAscii(dto.Email!);

            var entity = await dbSetBazaarSellerRegistration
                .Where(e => e.BazaarEventId == eventId && e.Email == email)
                .FirstOrDefaultAsync(cancellationToken);

            var hasFound = entity != null;

            if (!hasFound)
            {
                entity = new BazaarSellerRegistration();
                if (!dto.MapToEntity(entity))
                {
                    _logger.LogError("Create BazaarSellerRegistration for event {Id} failed", eventId);
                    return default;
                }
                entity.Id = _pkGenerator.Generate();
                entity.BazaarEventId = eventId;
            }
            else
            {
                if (!dto.MapToEntity(entity!))
                {
                    _logger.LogWarning("Update BazaarSellerRegistration for event {Id} failed", eventId);
                    return entity!.Id;
                }
            }

            if (!hasFound)
            {
                await dbSetBazaarSellerRegistration.AddAsync(entity!, cancellationToken);
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            return result ? entity!.Id : default;
        }
        finally
        {
            _registerSemaphore.Release();
        }
    }

    public async Task<bool> Register(Guid eventId, string email, string name, string phone, CancellationToken cancellationToken)
    {
        email = new IdnMapping().GetAscii(email);

        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        var entity = await dbSetBazaarSellerRegistration
            .Where(e => e.BazaarEventId == eventId && e.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is not null) return false;

        entity = new BazaarSellerRegistration
        {
            Id = _pkGenerator.Generate(),
            BazaarEventId = eventId,
            Email = email,
            Name = name,
            Phone = phone,
            PreferredType = 0
        };

        await dbSetBazaarSellerRegistration.AddAsync(entity, cancellationToken);
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public Task<bool> NotifyRegistration(Guid registrationId, CancellationToken cancellationToken)
    {
        /*var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = await dbSetBazaarSellerRegistration
            .Include(e => e.BazaarSeller)
            .Where(e => e.Id == registrationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            _logger.LogWarning("Registration {Id} not found", registrationId);
            return false;
        }

        if (!entity.Accepted.HasValue)
        {
            _logger.LogWarning("Registration {Id} has no value for accepted", registrationId);
            return false;
        }

        var hasChanges = false;
        var dbSetAccountNotification = _dbContext.Set<AccountNotification>();

        var hasPendingNotification = await dbSetAccountNotification
            .AsNoTracking()
            .AnyAsync(e => e.ReferenceId == registrationId && e.SentOn == null, cancellationToken);

        if (!hasPendingNotification)
        {
            await dbSetAccountNotification.AddAsync(new AccountNotification
            {
                Id = _pkGenerator.Generate(),
                Type = (int)BazaarEmailTemplate.AcceptSeller,
                CreatedOn = DateTimeOffset.UtcNow,
                ReferenceId = registrationId,
            }, cancellationToken);

            hasChanges = true;
        }

        // registration has been accepted but user is new
        if (entity.BazaarSeller != null && !entity.BazaarSeller.UserId.HasValue)
        {
            var userId = await _users.CreateSeller(entity.Email!, entity.Name!, cancellationToken);
            if (!userId.HasValue) return false;

            entity.BazaarSeller.UserId = userId;
            hasChanges = true;
        }

        if (hasChanges)
        {
            if (await _dbContext.SaveChangesAsync(cancellationToken) < 1) return false;

            await transaction.CommitAsync(cancellationToken);
        }
        */
        return Task.FromResult(false);
    }
}
