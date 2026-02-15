using FluentMigrator.Runner;
using GtKram.Infrastructure;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GtKram.Application.Tests;

public sealed class ServiceFixture : IAsyncDisposable
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _serviceProvider;
    private readonly string _databaseFile;

    public IServiceCollection Services => _services;

    public ServiceFixture()
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        _databaseFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".sqlite");
        connectionStringBuilder.DataSource = _databaseFile;
        connectionStringBuilder.ForeignKeys = true;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:SQLite", connectionStringBuilder.ToString() }
            })
            .Build();

        configuration.InitSQLiteContext();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddScoped<SQLiteDbContext>();
        _services.AddSingleton<TableLocker>();

        _services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(connectionStringBuilder.ToString())
                .ScanIn(typeof(Infrastructure.Database.Migrations.Initial).Assembly).For.Migrations());

        _services.AddSingleton(TimeProvider.System);
        _services.AddScoped(typeof(ISqlRepository<,>), typeof(SqlRepository<,>));

        _services.AddDataProtection();

        _services.AddScoped<ILookupNormalizer, NoneLookupNormalizer>();

        var builder = _services
            .AddIdentityCore<Infrastructure.Database.Models.Identity>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<Infrastructure.Database.Models.Identity>>(ConfirmEmailDataProtectionTokenProviderOptions.ProviderName);

        builder.AddUserStore<IdentityUserStore>();
        builder.AddSignInManager<SignInManager<Infrastructure.Database.Models.Identity>>();
        builder.Services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<Infrastructure.Database.Models.Identity>>();

        _services.AddMediatorHandler();
    }

    public IServiceProvider Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider;
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }
}
