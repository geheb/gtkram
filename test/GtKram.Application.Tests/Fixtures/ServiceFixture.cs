using FluentMigrator.Runner;
using GtKram.Infrastructure;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Repositories;
using GtKram.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GtKram.Application.Tests.Fixtures;

public sealed class ServiceFixture : IAsyncDisposable
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _serviceProvider;

    public IServiceCollection Services => _services;

    public ServiceFixture(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:gtkram", connectionString }
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);

        _services.AddSingleton(TimeProvider.System);

        _services.AddPersistence(configuration);

        _services.AddDataProtection();

        _services.AddScoped<ILookupNormalizer, UpperLookupNormalizer>();

        var builder = _services
            .AddIdentityCore<Infrastructure.Database.Models.Identity>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<Infrastructure.Database.Models.Identity>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        builder.AddUserStore<IdentityUserStore>();
        builder.AddSignInManager<SignInManager<Infrastructure.Database.Models.Identity>>();
        builder.Services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<Infrastructure.Database.Models.Identity>>();

        _services.AddMediatorHandler();
    }

    public void Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
    }

    public AsyncServiceScope CreateScope()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service Provider is not initialized");
        }
        return _serviceProvider.CreateAsyncScope();
    }

    public async Task MigrateDb(CancellationToken cancellationToken)
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service Provider is not initialized");
        }
        await using var scope = _serviceProvider.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
