using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GtKram.Application.Tests;

public sealed class ServiceFixture : IDisposable
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _serviceProvider;

    public IServiceCollection Services => _services;

    public ServiceFixture()
    {
        var prefix = Guid.NewGuid().ToString();
        _services.AddSingleton(TimeProvider.System);
        _services.AddScoped<IRepository<Identity>>(s => new RepositoryMock<Identity>(prefix));
        _services.AddScoped<IRepository<EmailQueue>>(s => new RepositoryMock<EmailQueue>(prefix));
        _services.AddScoped<IRepository<Event>>(s => new RepositoryMock<Event>(prefix));
        _services.AddScoped<IRepository<SellerRegistration>>(s => new RepositoryMock<SellerRegistration>(prefix));
        _services.AddScoped<IRepository<Seller>>(s => new RepositoryMock<Seller>(prefix));
        _services.AddScoped<IRepository<Article>>(s => new RepositoryMock<Article>(prefix));
        _services.AddScoped<IRepository<Checkout>>(s => new RepositoryMock<Checkout>(prefix));

        _services.AddDataProtection();

        _services.AddScoped<ILookupNormalizer, NoneLookupNormalizer>();

        var builder = _services
            .AddIdentityCore<Identity>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<Identity>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        builder.AddUserStore<IdentityUserStore>();
        builder.AddSignInManager<SignInManager<Identity>>();
        builder.Services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<Identity>>();

        _services.AddMediatorHandler();
    }

    public IServiceProvider Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider;
    }

    public void Dispose() => _serviceProvider?.Dispose();
}
