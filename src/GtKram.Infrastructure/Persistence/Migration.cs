using Dapper;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Repositories;
using GtKram.Infrastructure.User;
using System.Data.Common;
using System.Security.Claims;

namespace GtKram.Infrastructure.Persistence;

internal sealed class Migration
{
    private readonly MySqlDbContext _dbContext;
    private readonly IRepository<Entities.Event> _eventRepository;
    private readonly IRepository<Entities.Identity> _identityRepository;
    private readonly IRepository<Entities.SellerRegistration> _sellerRegistationRepository;
    private readonly IRepository<Entities.Seller> _sellerRepository;
    private readonly IRepository<Entities.Article> _articleRepository;
    private readonly IRepository<Entities.Checkout> _checkoutRepository;

    public Migration(
        MySqlDbContext dbContext,
        IRepository<Entities.Event> eventRepository,
        IRepository<Entities.Identity> identityRepository,
        IRepository<Entities.SellerRegistration> sellerRegistationRepository,
        IRepository<Entities.Article> articleRepository,
        IRepository<Entities.Checkout> checkoutRepository,
        IRepository<Entities.Seller> sellerRepository)
    {
        _dbContext = dbContext;
        _eventRepository = eventRepository;
        _identityRepository = identityRepository;
        _sellerRegistationRepository = sellerRegistationRepository;
        _articleRepository = articleRepository;
        _checkoutRepository = checkoutRepository;
        _sellerRepository = sellerRepository;
    }

    internal async Task Migrate(CancellationToken cancellationToken)
    {
        var connection = await _dbContext.GetConnection(cancellationToken);
        await InsertIdentities(connection, cancellationToken);
        await InsertEvents(connection, cancellationToken);
        await InsertSellerRegistration(connection, cancellationToken);
        await InsertSeller(connection, cancellationToken);
        await InsertArticles(connection,  cancellationToken);
        await InsertCheckouts(connection, cancellationToken);
    }

    private async Task InsertCheckouts(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _checkoutRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _checkoutRepository.BeginTransaction(cancellationToken);

        var checkouts = await connection.QueryAsync("select Id,BazaarEventId,Status,UserId,CreatedOn from bazaar_billings", transaction: trans);
        foreach (var e in checkouts)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[] userId = e.UserId;

            var checkout = new Entities.Checkout
            {
                Id = id.FromBinary16(),
                EventId = eventId.FromBinary16(),
                Status = e.Status,
                UserId = userId.FromBinary16(),
                Created = new DateTimeOffset((DateTime)e.CreatedOn, TimeSpan.Zero)
            };

            var articles = await connection.QueryAsync<byte[]>("select BazaarSellerArticleId from bazaar_billing_articles where BazaarBillingId=@id", new { id }, trans);
            checkout.ArticleIds = articles.Select(id => id.FromBinary16()).ToArray();

            await _checkoutRepository.Create(checkout, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }

    private async Task InsertArticles(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _articleRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _articleRepository.BeginTransaction(cancellationToken);

        var articles = await connection.QueryAsync("select Id,BazaarSellerId,LabelNumber,Name,Size,Price,CreatedOn from bazaar_seller_articles", transaction: trans);
        foreach (var e in articles)
        {
            byte[] id = e.Id;
            byte[] sellerId = e.BazaarSellerId;

            await _articleRepository.Create(new()
            {
                Id = id.FromBinary16(),
                SellerId = sellerId.FromBinary16(),
                LabelNumber = e.LabelNumber,
                Name = e.Name,
                Size = e.Size,
                Price = e.Price,
                Created = new DateTimeOffset((DateTime)e.CreatedOn, TimeSpan.Zero)
            }, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }


    private async Task InsertSeller(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _sellerRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _sellerRepository.BeginTransaction(cancellationToken);

        var sellers = await connection.QueryAsync("select Id,BazaarEventId,UserId,SellerNumber,Role,CanCreateBillings,MaxArticleCount from bazaar_sellers", transaction: trans);
        foreach (var e in sellers)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[] userId = e.UserId;

            await _sellerRepository.Create(new()
            {
                Id = id.FromBinary16(),
                EventId = eventId.FromBinary16(),
                UserId = userId.FromBinary16(),
                SellerNumber = e.SellerNumber,
                Role = e.Role,
                CanCheckout = e.CanCreateBillings,
                MaxArticleCount = e.MaxArticleCount
            }, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }

    private async Task InsertSellerRegistration(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _sellerRegistationRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _sellerRegistationRepository.BeginTransaction(cancellationToken);

        var registrations = await connection.QueryAsync("select Id,BazaarEventId,Email,Name,Phone,Clothing,Accepted,BazaarSellerId,PreferredType,CreatedOn from bazaar_seller_registrations", transaction: trans);
        foreach (var e in registrations)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[]? sellerId = e.BazaarSellerId;

            await _sellerRegistationRepository.Create(new()
            {
                Id = id.FromBinary16(),
                EventId = eventId.FromBinary16(),
                Email = e.Email,
                Name = e.Name,
                Phone = e.Phone,
                Clothing = e.Clothing,
                Accepted = e.Accepted,
                SellerId = sellerId?.FromBinary16(),
                PreferredType = e.PreferredType,
                Created = new DateTimeOffset((DateTime)e.CreatedOn, TimeSpan.Zero)
            }, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }

    private async Task InsertIdentities(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _identityRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _identityRepository.BeginTransaction(cancellationToken);

        var users = await connection.QueryAsync("select Id,Name,LastLogin,Email,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp from users", transaction: trans);
        foreach (var e in users)
        {
            byte[] id = e.Id;

            var claims = new List<Entities.IdentityClaim>();
            
            var roles = await connection.QueryAsync<string>("select Name from roles r join user_roles ur on ur.RoleId=r.Id where ur.UserId=@id", new { id }, trans);
            foreach (var role in roles)
            {
                claims.Add(new Entities.IdentityClaim(ClaimsIdentity.DefaultRoleClaimType, role == "billing" ? Roles.Checkout : role));
            }

            var tfa = await connection.QueryFirstOrDefaultAsync<string?>("select ClaimValue from user_claims where UserId=@id", new { id }, trans);
            if (tfa == "1")
            {
                claims.Add(new Entities.IdentityClaim(UserClaims.TwoFactorClaim));
            }

            var authKey = await connection.QueryFirstOrDefaultAsync<string?>("select Value from user_tokens where UserId=@id and Name=\"AuthenticatorKey\"", new { id }, trans);

            await _identityRepository.Create(new()
            {
                Id = id.FromBinary16(),
                UserName = id.FromBinary16().ToChar32(),
                Name = e.Name,
                LastLogin = e.LastLogin is not null ? new DateTimeOffset((DateTime)e.LastLogin, TimeSpan.Zero) : null,
                Email = e.Email,
                IsEmailConfirmed = e.EmailConfirmed,
                PasswordHash = e.PasswordHash,
                SecurityStamp = e.SecurityStamp,
                ConcurrencyStamp = e.ConcurrencyStamp,
                IsLockoutEnabled = true,
                Claims = claims,
                AuthenticatorKey = authKey,
                Created = e.LastLogin is not null ? new DateTimeOffset((DateTime)e.LastLogin, TimeSpan.Zero) : null,

            }, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }

    public async Task InsertEvents(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _eventRepository.Count([], cancellationToken);
        if (count > 0)
        {
            return;
        }

        var trans = await _eventRepository.BeginTransaction(cancellationToken);

        var events = await connection.QueryAsync("select Id,Name,Description,StartDate,EndDate,Address,MaxSellers,Commission,RegisterStartDate,RegisterEndDate,IsRegistrationsLocked,EditArticleEndDate,PickUpLabelsStartDate,PickUpLabelsEndDate,CreatedOn from bazaar_events", transaction: trans);
        foreach(var e in events)
        {
            byte[] id = e.Id;
            DateTime? pickUpLabelsStartDate = e.PickUpLabelsStartDate;
            DateTime? pickUpLabelsEndDate = e.PickUpLabelsEndDate;
            await _eventRepository.Create(new()
            {
                Id = id.FromBinary16(),
                Name = e.Name,
                Description = e.Description,
                Start = new DateTimeOffset((DateTime)e.StartDate,TimeSpan.Zero),
                End = new DateTimeOffset((DateTime)e.EndDate, TimeSpan.Zero),
                Address = e.Address,
                MaxSellers = e.MaxSellers,
                Commission = e.Commission,
                RegisterStart = new DateTimeOffset((DateTime)e.RegisterStartDate, TimeSpan.Zero),
                RegisterEnd = new DateTimeOffset((DateTime)e.RegisterEndDate, TimeSpan.Zero),
                HasRegistrationsLocked = e.IsRegistrationsLocked,
                EditArticleEnd = new DateTimeOffset((DateTime)e.EditArticleEndDate, TimeSpan.Zero),
                PickUpLabelsStart = 
                    pickUpLabelsStartDate is null ? null : 
                    new DateTimeOffset(pickUpLabelsStartDate.Value, TimeSpan.Zero),
                PickUpLabelsEnd =
                    pickUpLabelsEndDate is null ? null :
                    new DateTimeOffset(pickUpLabelsEndDate.Value, TimeSpan.Zero),
                Created = new DateTimeOffset((DateTime)e.CreatedOn, TimeSpan.Zero)
            }, trans, cancellationToken);
        }

        await trans.CommitAsync(cancellationToken);
    }
}
