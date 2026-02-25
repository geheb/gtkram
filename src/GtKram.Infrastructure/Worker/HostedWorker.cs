using Dapper;
using FluentMigrator.Runner;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace GtKram.Infrastructure.Worker;

internal sealed class HostedWorker : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public HostedWorker(
        ILogger<HostedWorker> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await HandleMigration(stoppingToken);
        await HandleSuperUser();

        while (!stoppingToken.IsCancellationRequested)
        {
            await HandleEmails(stoppingToken);

            await Task.Delay(30000, stoppingToken);
        }
    }

    private async Task HandleMigration(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SQLiteDbContext>();
        var connection = await dbContext.GetConnection(cancellationToken);

        _logger.LogInformation("Run database maintenance ...");
        await connection.ExecuteAsync("VACUUM;");
        await connection.ExecuteAsync("REINDEX;");

        _logger.LogInformation("Optimze database ...");
        await connection.ExecuteAsync("PRAGMA journal_mode = WAL;");
        await connection.ExecuteAsync("PRAGMA synchronous = NORMAL;");
        await connection.ExecuteAsync("PRAGMA cache_size = 10000;");
        await connection.ExecuteAsync("PRAGMA temp_store = MEMORY;");

        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        if (runner.HasMigrationsToApplyUp())
        {
            _logger.LogInformation("Run database migration ...");
            runner.MigrateUp();
        }
    }

    private async Task HandleSuperUser()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var contextInitializer = scope.ServiceProvider.GetRequiredService<DbContextInitializer>();
        await contextInitializer.CreateSuperAdmin();
    }

    private async Task HandleEmails(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var emailQueueRepository = scope.ServiceProvider.GetRequiredService<EmailQueues>();
        var smtpDispatcher = scope.ServiceProvider.GetRequiredService<SmtpDispatcher>();

        var models = await emailQueueRepository.GetNotSent(30, cancellationToken);

        foreach (var model in models)
        {
            try
            {
                Attachment? attachment = null;
                if (model.AttachmentBlob?.Length > 0)
                {
                    attachment = new(new MemoryStream(model.AttachmentBlob), model.AttachmentName, model.AttachmentMimeType);
                }

                await smtpDispatcher.Send(model.Recipient, model.Subject, model.Body, attachment);

                var result = await emailQueueRepository.UpdateSent(model.Id, cancellationToken);
                if (result.IsError)
                {
                    _logger.LogError("Update der EmailQueue {Id} fehlgeschlagen: {Error}",
                        model.Id,
                        string.Join(",", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Emailversand {Id} fehlgeschlagen.", model.Id);
            }
        }
    }
}
