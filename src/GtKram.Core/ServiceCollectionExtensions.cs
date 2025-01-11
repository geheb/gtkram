namespace GtKram.Core;

using GtKram.Core.Database;
using GtKram.Core.Email;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using GtKram.Core.Worker;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddCore(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SmtpConnectionOptions>(config.GetSection("Smtp"));

        services.AddHostedService<HostedWorker>();

        services.AddSingleton<EmailValidatorService>();
        services.AddSingleton<SmtpDispatcher>();

        services.AddScoped<IdentityErrorDescriber, GermanyIdentityErrorDescriber>();
        services.AddScoped<AppDbContextInitializer>();
        services.AddScoped<BazaarBillingArticles>();
        services.AddScoped<BazaarBillings>();
        services.AddScoped<BazaarEvents>();
        services.AddScoped<BazaarSellerArticles>();
        services.AddScoped<BazaarSellers>();
        services.AddScoped<EmailAuth>();
        services.AddScoped<SellerRegistrations>();
        services.AddScoped<TwoFactorAuth>();
        services.AddScoped<Users>();
    }
}
