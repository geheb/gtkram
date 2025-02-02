using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrations : ISellerRegistrations, IDisposable
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly Domain.Repositories.IUserRepository _userRepository;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _registerSemaphore = new SemaphoreSlim(1, 1);

    public SellerRegistrations(
        AppDbContext dbContext,
        IMediator mediator,
        Domain.Repositories.IUserRepository userRepository,
        ILogger<SellerRegistrations> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _userRepository = userRepository;
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

    public async Task<bool> Confirm(Guid eventId, Guid registrationId, bool confirmed, string? registerUserCallbackUrl, CancellationToken cancellationToken)
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
                    Role = (int)Domain.Models.SellerRole.Standard,
                    MaxArticleCount = BazaarSellers.CalcMaxArticleCount(Domain.Models.SellerRole.Standard)
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

        return await NotifyRegistration(entity.Id, registerUserCallbackUrl, cancellationToken);
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
                    return default;
                }
                entity.Id = _pkGenerator.Generate();
                entity.BazaarEventId = eventId;
            }
            else
            {
                if (!dto.MapToEntity(entity!))
                {
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

    private async Task<bool> NotifyRegistration(Guid registrationId, string? registerUserCallbackUrl, CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerRegistration = _dbContext.Set<BazaarSellerRegistration>();

        var entity = await dbSetBazaarSellerRegistration
            .Include(e => e.BazaarSeller)
            .Where(e => e.Id == registrationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity?.Accepted is null)
        {
            return false;
        }

        if (entity.BazaarSeller!.UserId is null &&
            entity.Accepted == true &&
            registerUserCallbackUrl is not null)
        {
            var userResult = await _userRepository.FindByEmail(entity.Email!, cancellationToken);
            if (userResult.IsSuccess)
            {
                entity.BazaarSeller.UserId = userResult.Value.Id;
            }
            else
            {
                var idResult = await _mediator.Send(new CreateUserCommand(entity.Name!, entity.Email!, [Domain.Models.UserRoleType.Seller], registerUserCallbackUrl), cancellationToken);
                if (idResult.IsFailed)
                {
                    return false;
                }
                entity.BazaarSeller.UserId = idResult.Value;
            }

            if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
            {
                return false;
            }
        }

        if (entity.Accepted == true)
        {
            // await _mediator.Send(new SendAcceptSellerEmailCommand(entity.BazaarSeller.UserId!.Value, entity.BazaarEventId!.Value), cancellationToken);
        }
        else
        {
           // await _mediator.Send(new SendDenySellerEmailCommand(entity.Email!, entity.Name!, entity.BazaarEventId!.Value), cancellationToken);
        }

        return true;
    }
}
