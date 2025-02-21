using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GtKram.Infrastructure.Persistence;

namespace GtKram.Application.Tests;

public abstract class DatabaseFixture : IAsyncLifetime
{
    protected ServiceProvider _serviceProvider = null!;

    public async Task DisposeAsync()
    {
       await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Setup(services);

        _serviceProvider = services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    protected abstract void Setup(IServiceCollection services);
}
