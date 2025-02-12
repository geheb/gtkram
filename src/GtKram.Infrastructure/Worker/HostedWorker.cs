using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
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
        await HandleSuperUser();

        while (!stoppingToken.IsCancellationRequested)
        {
            await HandleEmails(stoppingToken);

            await Task.Delay(30000, stoppingToken);
        }
    }

    private async Task HandleSuperUser()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var contextInitializer = scope.ServiceProvider.GetRequiredService<AppDbContextInitializer>();
        await contextInitializer.CreateSuperAdmin();
    }

    private async Task HandleEmails(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var emailQueueRepository = scope.ServiceProvider.GetRequiredService<EmailQueueRepository>();
        var smtpDispatcher = scope.ServiceProvider.GetRequiredService<SmtpDispatcher>();

        var emailEntities = await emailQueueRepository.GetBySentOnIsNull(cancellationToken);

        foreach (var entity in emailEntities)
        {
            try
            {
                Attachment? attachment = null;
                if (entity.AttachmentBlob?.Length > 0)
                {
                    attachment = new(new MemoryStream(entity.AttachmentBlob), entity.AttachmentName, entity.AttachmentMimeType);
                }

                await smtpDispatcher.Send(entity.Recipient!, entity.Subject!, entity.Body!, attachment);

                var result = await emailQueueRepository.UpdateSentOn(entity.Id, cancellationToken);
                if (result.IsFailed)
                {
                    _logger.LogError(string.Join(", ", result.Errors.Select(e => e.Message)));
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Sending email {Id} failed.", entity.Id);
            }
        }
    }
}
