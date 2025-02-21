using GtKram.Application.Services;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using GtKram.Infrastructure.User;
using GtKram.Infrastructure.Worker;
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

        services.Configure<SmtpConnectionOptions>(config.GetSection("Smtp"));

        services.AddHostedService<HostedWorker>();
        services.AddScoped<SmtpDispatcher>();
        services.AddSingleton<IEmailValidatorService, EmailValidatorService>();
        services.AddScoped<IUserAuthenticator, UserAuthenticator>();
        services.AddScoped<IdentityErrorDescriber, GermanyIdentityErrorDescriber>();

        services.AddScoped<EmailQueueRepository>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBazaarEventRepository, BazaarEventRepository>();
        services.AddScoped<IBazaarSellerRegistrationRepository, BazaarSellerRegistrationRepository>();
        services.AddScoped<IBazaarBillingRepository, BazaarBillingRepository>();
        services.AddScoped<IBazaarBillingArticleRepository, BazaarBillingArticleRepository>();
        services.AddScoped<IBazaarSellerArticleRepository, BazaarSellerArticleRepository>();
        services.AddScoped<IBazaarSellerRepository, BazaarSellerRepository>();
    }

    public static void AddAuthorizationWith2FA(this IServiceCollection services, string name)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(name, policy => policy.RequireClaim(UserClaims.TwoFactorClaim.Type, UserClaims.TwoFactorClaim.Value));
        });
    }

    public static void ConfigureDataProtection(this IServiceCollection services)
    {
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
    }

    public static void AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddMySqlContext(config);
        
        services
            .AddIdentity<IdentityUserGuid, IdentityRoleGuid>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = ConfirmEmailDataProtectionTokenProviderOptions.ProviderName;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<IdentityUserGuid>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        services.AddDataProtection()
            .AddCertificate(config.GetSection("DataProtection"));

        services.AddScoped<AppDbContextInitializer>();
    }

    public static void AddSmtp(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SmtpConnectionOptions>(config.GetSection("Smtp"));
        services.AddScoped<SmtpDispatcher>();
    }
}
