using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using GtKram.Infrastructure.User;
using GtKram.Infrastructure.Worker;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GtKram.Infrastructure;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<HostedWorker>();

        services.Configure<SmtpConnectionOptions>(config.GetSection("Smtp"));
        services.AddSingleton<IEmailValidatorService, EmailValidatorService>();
        services.AddScoped<EmailQueueRepository>();
        services.AddScoped<SmtpDispatcher>();
        services.AddScoped<IUserAuthenticator, UserAuthenticator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISellerRegistrationRepository, SellerRegistrationRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<ISellerRepository, SellerRepository>();
    }

    public static void AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<MySqlBootstrapper>();
        services.AddScoped<MySqlDbContext>();

        services.AddScoped<IRepository<EmailQueue>, Repository<EmailQueue>>();
        services.AddScoped<IRepository<Identity>, Repository<Identity>>();
        services.AddScoped<IRepository<Event>, Repository<Event>>();
        services.AddScoped<IRepository<SellerRegistration>, Repository<SellerRegistration>>();
        services.AddScoped<IRepository<Seller>, Repository<Seller>>();
        services.AddScoped<IRepository<Article>, Repository<Article>>();
        services.AddScoped<IRepository<Checkout>, Repository<Checkout>>();

        services.AddScoped<DbContextInitializer>();
    }

    public static void AddAuth(this IServiceCollection services, IConfiguration config, string policyName)
    {
        services.AddScoped<ILookupNormalizer, NoneLookupNormalizer>();
        services.AddScoped<IdentityErrorDescriber, GermanyIdentityErrorDescriber>();

        var builder = services
            .AddIdentityCore<Identity>(o =>
            {
                o.SignIn.RequireConfirmedEmail = true;
                o.Tokens.EmailConfirmationTokenProvider = ConfirmEmailDataProtectionTokenProviderOptions.ProviderName;
            })
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<Identity>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        builder.AddUserStore<IdentityUserStore>();
        builder.AddSignInManager<SignInManager<Identity>>();
        builder.Services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<Identity>>();

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
}
