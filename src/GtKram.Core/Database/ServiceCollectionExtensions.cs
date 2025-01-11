using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GtKram.Core.Database;

public static class ServiceCollectionExtensions
{
    public static void AddMySqlContext(this IServiceCollection services, IConfiguration configuration)
    {
        var assemblyName = typeof(AppDbContext).GetTypeInfo().Assembly.GetName().Name;

        services.AddDbContext<AppDbContext>(options =>
        {
            options.ConfigureWarnings(warn => warn.Ignore(
                CoreEventId.FirstWithoutOrderByAndFilterWarning,
                CoreEventId.RowLimitingOperationWithoutOrderByWarning,
                CoreEventId.DistinctAfterOrderByWithoutRowLimitingOperatorWarning));

            options.UseMySql(
                configuration.GetConnectionString("MySql"),
                MariaDbServerVersion.LatestSupportedServerVersion,
                mysqlOptions =>
                {
                    mysqlOptions.MaxBatchSize(100);
                    mysqlOptions.MigrationsAssembly(assemblyName);
                });
        });
    }
}
