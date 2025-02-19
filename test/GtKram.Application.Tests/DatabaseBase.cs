using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GtKram.Infrastructure.Persistence;

namespace GtKram.Application.Tests;

public abstract class DatabaseBase
{
    protected readonly IServiceProvider _serviceProvider;

    public DatabaseBase()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
    }

    protected abstract void ConfigureServices(IServiceCollection services);
}