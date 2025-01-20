using GtKram.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GtKram.Application;

public static class ServiceExtensions
{
    public static void AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AppSettings>(config.GetSection("App"));

        services.AddMediator(options =>
        {
            options.Namespace = "GtKram.Application.MediatorGenerated";
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
    }
}
