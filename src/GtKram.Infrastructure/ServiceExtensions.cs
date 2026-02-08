using Dapper;
using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Repositories;
using GtKram.Infrastructure.Security;
using GtKram.Infrastructure.Worker;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data;

namespace GtKram.Infrastructure;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<HostedWorker>();

        services.Configure<SmtpConnectionOptions>(configuration.GetSection("Smtp"));
        services.AddSingleton<BlacklistCache>();
        services.AddSingleton<IpReputationChecker>();
        services.AddSingleton<IEmailValidator, EmailValidator>();
        services.AddSingleton<SmtpDispatcher>();

        services.AddScoped<IUserAuthenticator, UserAuthenticator>();
        services.AddScoped<IEmailService, EmailService>();
    }

    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<MigrationHealthCheck>("migration");

        configuration.InitSQLiteContext();

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(configuration.GetConnectionString("SQLite"))
                .WithVersionTable(new Migrations())
                .ScanIn(typeof(Database.Migrations.Initial).Assembly).For.Migrations());

        services.AddScoped<SQLiteDbContext>();

        services.AddSingleton<TableLocker>();
        services.AddScoped(typeof(Database.Repositories.ISqlRepository<>), typeof(Database.Repositories.SqlRepository<>));
        services.AddScoped<EmailQueues>();
        services.AddScoped<IUsers, Users>();
        services.AddScoped<IEvents, Events>();
        services.AddScoped<ISellerRegistrations, SellerRegistrations>();
        services.AddScoped<ISellers, Sellers>();
        services.AddScoped<IArticles, Articles>();
        services.AddScoped<ICheckouts, Checkouts>();
        services.AddScoped<IPlannings, Plannings>();

        services.AddScoped<DbContextInitializer>();
    }

    public static void AddAuth(this IServiceCollection services, IConfiguration config, string policyName)
    {
        services.AddScoped<ILookupNormalizer, NoneLookupNormalizer>();
        services.AddScoped<IdentityErrorDescriber, GermanyIdentityErrorDescriber>();

        var builder = services
            .AddIdentityCore<Database.Models.Identity>(o =>
            {
                o.SignIn.RequireConfirmedEmail = true;
                o.Tokens.EmailConfirmationTokenProvider = ConfirmEmailDataProtectionTokenProviderOptions.ProviderName;
            })
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<Database.Models.Identity>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        builder.AddUserStore<Database.Repositories.IdentityUserStore>();
        builder.AddSignInManager<SignInManager<Database.Models.Identity>>();
        builder.Services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<Database.Models.Identity>>();

        services.AddDataProtection()
            .AddCertificate(config);

        var auth = services.AddAuthentication(IdentityConstants.ApplicationScheme);
        auth.AddApplicationCookie();
        auth.AddTwoFactorRememberMeCookie();
        auth.AddTwoFactorUserIdCookie();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(policyName, policy => policy.RequireClaim(UserClaims.TwoFactorClaim.Type, UserClaims.TwoFactorClaim.Value));
        });

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10; // see PasswordLengthFieldAttribute
            options.Password.RequiredUniqueChars = 5;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(3);
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        });

        services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieNames.AppToken;
            options.ExpireTimeSpan = TimeSpan.FromHours(1);

            options.LoginPath = "/Login";
            options.LogoutPath = "/Login/Exit";
            options.AccessDeniedPath = "/Error/403";
            options.SlidingExpiration = true;
        });

        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieNames.TwoFactorTrustToken;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
        });

        services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieNames.TwoFactorIdToken;
        });

        services.Configure<ConfirmEmailDataProtectionTokenProviderOptions>(options =>
        {
            // lifespan of issued tokens for new users
            options.TokenLifespan = TimeSpan.FromDays(5);
        });

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            // lifespan of issued tokens for changing email or password
            options.TokenLifespan = TimeSpan.FromDays(1);
        });

        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            // A user accessing the site with an existing cookie would be validated, and a new cookie would be issued. 
            // This process is completely silent and happens behind the scenes.
            options.ValidationInterval = TimeSpan.FromMinutes(30);
        });

        services.Configure<AntiforgeryOptions>(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = CookieNames.XcsrfToken;
        });
    }

    public static void AddSmtp(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SmtpConnectionOptions>(config.GetSection("Smtp"));
        services.AddScoped<SmtpDispatcher>();
    }

    internal static void InitSQLiteContext(this IConfiguration configuration)
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/dapper-limitations
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());

        var connectionStringBuilder = new SqliteConnectionStringBuilder(configuration.GetConnectionString("SQLite"));

        var file = new FileInfo(connectionStringBuilder.DataSource);
        if (file.Directory?.Exists == false)
        {
            file.Directory.Create();
        }
    }

    private sealed class Migrations : IVersionTableMetaData
    {
        public string SchemaName => string.Empty;
        public string TableName => "migrations";
        public string ColumnName => "Version";
        public string UniqueIndexName => "IX_migrations_Version";
        public string AppliedOnColumnName => "AppliedOn";
        public string DescriptionColumnName => "Description";
        public bool OwnsSchema => true;
        public bool CreateWithPrimaryKey => false;
    }

    private abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        public override void SetValue(IDbDataParameter parameter, T? value)
            => parameter.Value = value;
    }

    private sealed class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.Parse((string)value);
    }

    private sealed class GuidHandler : SqliteTypeHandler<Guid>
    {
        public override Guid Parse(object value)
            => Guid.Parse((string)value);
    }

    private sealed class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value)
            => TimeSpan.Parse((string)value);
    }
}
