using GtKram.Application.Converter;
using GtKram.Application.Options;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace GtKram.Infrastructure.Worker;

internal sealed class HostedWorker : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AccountEmailTemplateRenderer _accountEmailTemplateRenderer = new();
    private readonly BazaarEmailTemplateRenderer _bazaarEmailTemplateRenderer = new();
    private readonly TimeSpan _confirmEmailTimeout, _changeEmailPassTimeout;
    private readonly string? _appUrl;
    private readonly string _organizer;

    public HostedWorker(
        IOptions<ConfirmEmailDataProtectionTokenProviderOptions> confirmEmailOptions,
        IOptions<DataProtectionTokenProviderOptions> changeEmailPassOptions,
        IOptions<AppSettings> appSettingsOptions,
        ILogger<HostedWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _confirmEmailTimeout = confirmEmailOptions.Value.TokenLifespan;
        _changeEmailPassTimeout = changeEmailPassOptions.Value.TokenLifespan;
        _appUrl = appSettingsOptions.Value.PublicUrl;
        _organizer = appSettingsOptions.Value.Organizer;
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
        using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dataProtection = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var emailSender = scope.ServiceProvider.GetRequiredService<SmtpDispatcher>();

        var dbSetAccountNotification = dbContext.Set<AccountNotification>();
        var entities = await dbSetAccountNotification
            .Include(e => e.User)
            .Where(e => e.SentOn == null)
            .Take(32)
            .ToArrayAsync(cancellationToken);

        if (!entities.Any()) return;

        var count = 0;

        foreach (var entity in entities)
        {
            var template = (AccountEmailTemplate)entity.Type;

            /*if (template == BazaarEmailTemplate.AcceptSeller)
            {
                if (await HandleRegisterSeller(dbContext, entity, emailSender, cancellationToken))
                {
                    count++;
                }
                continue;
            }*/

            if (await HandleUser(entity, emailSender, dataProtection, cancellationToken))
            {
                count++;
            }
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GetTitle(AccountEmailTemplate template)
    {
        return template switch
        {
            AccountEmailTemplate.ConfirmRegistration => "Registrierung bestätigen",
            AccountEmailTemplate.ResetPassword => "Passwort vergessen",
            AccountEmailTemplate.ChangeEmail => "E-Mail-Adresse ändern",
            _ => throw new NotImplementedException($"unknown {nameof(AccountEmailTemplate)} {template}")
        };
    }

    private static string Format(TimeSpan span) => span.TotalDays > 1 ? $"{span.TotalDays} Tage" : $"{span.TotalHours} Stunden";

    private string GetTimeout(AccountEmailTemplate template)
    {
        switch (template)
        {
            case AccountEmailTemplate.ConfirmRegistration:
                return Format(_confirmEmailTimeout);
            default:
                return Format(_changeEmailPassTimeout);
        }
    }

    private async Task<bool> HandleUser(
        AccountNotification entity,
        SmtpDispatcher emailSender,
        IDataProtectionProvider dataProtection, 
        CancellationToken cancellationToken)
    {
        var template = (AccountEmailTemplate)entity.Type;

        var model = new
        {
            title = GetTitle(template),
            name = entity.User!.Name!.Split(' ')[0],
            link = entity.CallbackUrl,
            timeout = GetTimeout(template),
            signature = _organizer
        };

        var message = await _accountEmailTemplateRenderer.Render(template, model);
        var targetEmail = entity.User.Email!;

        if (template == AccountEmailTemplate.ChangeEmail)
        {
            var email = GetEmailFromUrl(dataProtection, entity.CallbackUrl!, entity.User.SecurityStamp!);
            if (!string.IsNullOrEmpty(email))
            {
                targetEmail = email;
            }
        }

        try
        {
            await emailSender.SendEmailAsync(targetEmail, model.title, message);
            entity.SentOn = DateTimeOffset.UtcNow;
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Send email {Template} for user {Id} failed", template, entity.User.Id);
        }

        return false;
    }

    private static string FormatDate(GermanDateTimeConverter dc, DateTimeOffset start, DateTimeOffset end)
    {
        var from = dc.ToDate(start) + " um " + dc.ToTime(start);
        var to = dc.ToDate(end) + " um " + dc.ToTime(end);
        if (start == end)
        {
            return from + " Uhr";
        }
        else if (start.Date == end.Date)
        {
            return from + " - " + dc.ToTime(end) + " Uhr";
        }
        return from + " Uhr - " + to + " Uhr";
    }

    private async Task<bool> HandleRegisterSeller(
        AppDbContext dbContext, 
        AccountNotification entity,
        SmtpDispatcher emailSender,
        CancellationToken cancellationToken)
    {
        var dbSetBazaarSellerRegistration = dbContext.Set<BazaarSellerRegistration>();

        var registration = await dbSetBazaarSellerRegistration
            .AsNoTracking()
            .Include(e => e.BazaarEvent)
            .FirstOrDefaultAsync(e => e.Id == entity.ReferenceId, cancellationToken);

        if (registration == null)
        {
            entity.SentOn = DateTimeOffset.MinValue;
            _logger.LogError("Seller registration {Id} not found", entity.ReferenceId);
            return true;
        }

        var template = registration.Accepted.GetValueOrDefault() ? BazaarEmailTemplate.AcceptSeller : BazaarEmailTemplate.DenySeller;
        var @event = registration.BazaarEvent!;

        var dc = new GermanDateTimeConverter();
        var start = dc.ToLocal(@event.StartDate);
        var end = dc.ToLocal(@event.EndDate);
        var editEndDate = dc.ToLocal(@event.EditArticleEndDate ?? @event.StartDate);
        var pickUpStart = dc.ToLocal(@event.PickUpLabelsStartDate ?? @event.StartDate);
        var pickUpEnd = dc.ToLocal(@event.PickUpLabelsEndDate ?? @event.StartDate);

        var model = new
        {
            title = $"Registrierung zum {@event.Name}",
            name = registration.Name!.Split(' ')[0],
            eventname = @event.Name,
            date = FormatDate(dc, start, end),
            address = @event.Address,
            editenddate = FormatDate(dc, editEndDate, editEndDate),
            appurl = _appUrl,
            signature = _organizer,
            pickupdate = FormatDate(dc, pickUpStart, pickUpEnd),
        };

        var calendarEvent = new CalendarEvent().Create("Abholung der Etiketten", @event.Address, pickUpStart, pickUpEnd);
        var attachment = Attachment.CreateAttachmentFromString(calendarEvent, "event.ics", Encoding.UTF8, CalendarEvent.MimeType);
        var message = await _bazaarEmailTemplateRenderer.Render(template, model);

        try
        {
            await emailSender.SendEmailAsync(registration.Email!, model.title, message, attachment);
            entity.SentOn = DateTimeOffset.UtcNow;
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Send email {Template} for seller registration {Id} failed", template, registration.Id);
        }

        return false;
    }

    private string? GetEmailFromUrl(IDataProtectionProvider dataProtection, string callbackUrl, string securityStamp)
    {
        var query = HttpUtility.ParseQueryString(callbackUrl);

        var encodedEmail = query.Get("email");
        if (string.IsNullOrEmpty(encodedEmail)) return null;

        var protector = dataProtection.CreateProtector(securityStamp);

        var decodedEmail = Encoding.UTF8.GetString(protector.Unprotect(Convert.FromBase64String(encodedEmail)));

        return decodedEmail;
    }
}
