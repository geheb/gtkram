using Dapper;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data.Common;
using System.Security.Claims;

namespace GtKram.Infrastructure.Worker;

internal sealed class MysqlMigration
{
    private readonly string _connectionStringMySql;
    private readonly SQLiteDbContext _sqlite;
    private readonly ISqlRepository<Database.Models.Event> _eventRepository;
    private readonly ISqlRepository<Database.Models.Identity> _identityRepository;
    private readonly ISqlRepository<Database.Models.SellerRegistration> _sellerRegistationRepository;
    private readonly ISqlRepository<Database.Models.Seller> _sellerRepository;
    private readonly ISqlRepository<Database.Models.Article> _articleRepository;
    private readonly ISqlRepository<Database.Models.Checkout> _checkoutRepository;

    public MysqlMigration(
        IConfiguration configuration,
        SQLiteDbContext sqlite,
        ISqlRepository<Database.Models.Event> eventRepository,
        ISqlRepository<Database.Models.Identity> identityRepository,
        ISqlRepository<Database.Models.SellerRegistration> sellerRegistationRepository,
        ISqlRepository<Database.Models.Article> articleRepository,
        ISqlRepository<Database.Models.Checkout> checkoutRepository,
        ISqlRepository<Database.Models.Seller> sellerRepository)
    {
        _connectionStringMySql = configuration.GetConnectionString("MySql")!;
        _sqlite = sqlite;
        _eventRepository = eventRepository;
        _identityRepository = identityRepository;
        _sellerRegistationRepository = sellerRegistationRepository;
        _articleRepository = articleRepository;
        _checkoutRepository = checkoutRepository;
        _sellerRepository = sellerRepository;
    }

    internal async Task Migrate(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionStringMySql);
        await connection.OpenAsync(cancellationToken);

        await InsertIdentities(connection, cancellationToken);
        await InsertEvents(connection, cancellationToken);
        await InsertSeller(connection, cancellationToken);
        await InsertSellerRegistration(connection, cancellationToken);
        await InsertArticles(connection,  cancellationToken);
        await InsertCheckouts(connection, cancellationToken);
    }

    private async Task InsertCheckouts(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _checkoutRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var checkouts = await connection.QueryAsync("select Id,BazaarEventId,Status,UserId,CreatedOn from bazaar_billings");
        foreach (var e in checkouts)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[] userId = e.UserId;

            var checkout = new Database.Models.Checkout
            {
                Id = new Guid(id),
                Created = (DateTime)e.CreatedOn,
                Json = new()
                {
                    EventId = new Guid(eventId),
                    Status = e.Status,
                    IdentityId = new Guid(userId),
                }
            };

            var articles = await connection.QueryAsync<byte[]>("select BazaarSellerArticleId from bazaar_billing_articles where BazaarBillingId=@id", new { id });
            checkout.Json.ArticleIds = articles.Select(id => new Guid(id)).ToArray();

            await _checkoutRepository.Insert(checkout, cancellationToken);
        }
    }

    private async Task InsertArticles(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _articleRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var articles = await connection.QueryAsync("select Id,BazaarSellerId,LabelNumber,Name,Size,Price,CreatedOn from bazaar_seller_articles");
        foreach (var e in articles)
        {
            byte[] id = e.Id;
            byte[] sellerId = e.BazaarSellerId;

            await _articleRepository.Insert(new()
            {
                Id = new Guid(id),
                Created = (DateTime)e.CreatedOn,
                Json = new()
                {
                    SellerId = new Guid(sellerId),
                    LabelNumber = e.LabelNumber,
                    Name = e.Name,
                    Size = e.Size,
                    Price = e.Price,
                }
            }, cancellationToken);
        }
    }


    private async Task InsertSeller(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _sellerRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var sellers = await connection.QueryAsync("select Id,BazaarEventId,UserId,SellerNumber,Role,CanCreateBillings,MaxArticleCount from bazaar_sellers");
        foreach (var e in sellers)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[] userId = e.UserId;

            await _sellerRepository.Insert(new()
            {
                Id = new Guid(id),
                Json = new()
                {
                    EventId = new Guid(eventId),
                    IdentityId = new Guid(userId),
                    SellerNumber = e.SellerNumber,
                    Role = e.Role,
                    CanCheckout = e.CanCreateBillings,
                    MaxArticleCount = e.MaxArticleCount
                }
            }, cancellationToken);
        }
    }

    private async Task InsertSellerRegistration(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _sellerRegistationRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var registrations = await connection.QueryAsync("select Id,BazaarEventId,Email,Name,Phone,Clothing,Accepted,BazaarSellerId,PreferredType,CreatedOn from bazaar_seller_registrations");
        foreach (var e in registrations)
        {
            byte[] id = e.Id;
            byte[] eventId = e.BazaarEventId;
            byte[]? sellerId = e.BazaarSellerId;

            await _sellerRegistationRepository.Insert(new()
            {
                Id = new Guid(id),
                Created = (DateTime)e.CreatedOn,
                Json = new()
                {
                    EventId = new Guid(eventId),
                    SellerId = sellerId is null ? null : new Guid(sellerId),
                    Email = e.Email,
                    Name = e.Name,
                    Phone = e.Phone,
                    Clothing = e.Clothing,
                    IsAccepted = e.Accepted,
                    PreferredType = e.PreferredType,
                }
            }, cancellationToken);
        }
    }

    private async Task InsertIdentities(DbConnection mysql, CancellationToken cancellationToken)
    {
        var count = await _identityRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var users = await mysql.QueryAsync("select Id,Name,LastLogin,Email,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp from users");
        foreach (var e in users)
        {
            byte[] id = e.Id;

            var claims = new List<Database.Models.IdentityClaim>();
            
            var roles = await mysql.QueryAsync<string>("select Name from roles r join user_roles ur on ur.RoleId=r.Id where ur.UserId=@id", new { id });
            foreach (var role in roles)
            {
                claims.Add(new Database.Models.IdentityClaim(ClaimTypes.Role, role == "billing" ? Roles.Checkout : role));
            }

            var tfa = await mysql.QueryFirstOrDefaultAsync<string?>("select ClaimValue from user_claims where UserId=@id", new { id });
            if (tfa == "1")
            {
                claims.Add(new Database.Models.IdentityClaim(UserClaims.TwoFactorClaim));
            }

            var authKey = await mysql.QueryFirstOrDefaultAsync<string?>("select Value from user_tokens where UserId=@id and Name=\"AuthenticatorKey\"", new { id });
            DateTime? lastLogin = e.LastLogin is not null ? (DateTime)e.LastLogin : null;

            await _identityRepository.Insert(new()
            {
                Id = new Guid(id),
                Created = lastLogin ?? DateTime.MinValue,
                Json = new()
                {
                    UserName = Guid.NewGuid().ToString("N"),
                    Name = e.Name,
                    LastLogin = lastLogin,
                    Email = e.Email,
                    IsEmailConfirmed = e.EmailConfirmed,
                    PasswordHash = e.PasswordHash,
                    SecurityStamp = e.SecurityStamp,
                    ConcurrencyStamp = e.ConcurrencyStamp,
                    IsLockoutEnabled = true,
                    Claims = claims,
                    AuthenticatorKey = authKey
                }
            }, cancellationToken);
        }
    }

    public async Task InsertEvents(DbConnection connection, CancellationToken cancellationToken)
    {
        var count = await _eventRepository.Count(cancellationToken);
        if (count > 0)
        {
            return;
        }

        var events = await connection.QueryAsync("select Id,Name,Description,StartDate,EndDate,Address,MaxSellers,Commission,RegisterStartDate,RegisterEndDate,IsRegistrationsLocked,EditArticleEndDate,PickUpLabelsStartDate,PickUpLabelsEndDate,CreatedOn from bazaar_events");
        foreach(var e in events)
        {
            byte[] id = e.Id;
            DateTime? pickUpLabelsStartDate = e.PickUpLabelsStartDate;
            DateTime? pickUpLabelsEndDate = e.PickUpLabelsEndDate;
            await _eventRepository.Insert(new Database.Models.Event()
            {
                Id = new Guid(id),
                Created = (DateTime)e.CreatedOn,
                Json = new()
                {
                    Name = e.Name,
                    Description = e.Description,
                    Start = new DateTimeOffset((DateTime)e.StartDate, TimeSpan.Zero),
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
                    
                }
            }, cancellationToken);
        }
    }
}
