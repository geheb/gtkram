using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GtKram.Infrastructure.Persistence;

namespace GtKram.Application.Tests;

public sealed class ServiceFixture : IDisposable
{
    private readonly ServiceCollection _services = new();
    private ServiceProvider? _serviceProvider;

    public IServiceCollection Services => _services;

    public ServiceFixture()
    {
        _services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    }

    public IServiceProvider Build()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider;
    }

    public void Dispose() => _serviceProvider?.Dispose();
}
