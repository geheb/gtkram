using FluentMigrator.Runner;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GtKram.Infrastructure.Database;

internal sealed class MigrationHealthCheck : IHealthCheck
{
    private readonly IMigrationRunner _runner;

    public MigrationHealthCheck(IMigrationRunner runner)
    {
        _runner = runner;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var hasMigrations = _runner.HasMigrationsToApplyUp();
        if (hasMigrations)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Missing migrations"));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Migrations applied"));
    }
}