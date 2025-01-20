using FluentResults;
using GtKram.Application.Converter;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text;

namespace GtKram.Infrastructure.Email;

internal sealed class EmailService : IEmailService
{
    private readonly AppSettings _appSettings;
    private readonly TimeProvider _timeProvider;
    private readonly EmailQueueRepository _repository;
    private readonly TimeSpan _confirmEmailTimeout;
    private readonly TimeSpan _changeEmailOrPasswordTimeout;

    public EmailService(
        TimeProvider timeProvider,
        EmailQueueRepository repository,
        IOptions<AppSettings> appSettings,
        IOptions<ConfirmEmailDataProtectionTokenProviderOptions> confirmEmailOptions,
        IOptions<DataProtectionTokenProviderOptions> changeEmailOrPasswordOptions)
    {
        _appSettings = appSettings.Value;
        _timeProvider = timeProvider;
        _repository = repository;
        _confirmEmailTimeout = confirmEmailOptions.Value.TokenLifespan;
        _changeEmailOrPasswordTimeout = changeEmailOrPasswordOptions.Value.TokenLifespan;
    }

    public async Task<Result> EnqueueChangeEmail(string email, string name, string callbackUrl, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var model = new
        {
            title = "E-Mail-Adresse ändern",
            name = name,
            link = callbackUrl,
            timeout = dc.Format(_changeEmailOrPasswordTimeout),
            signature = _appSettings.Organizer
        };

        var renderer = new AccountEmailTemplateRenderer();
        var message = await renderer.Render(AccountEmailTemplate.ChangeEmail, model);

        var entity = new EmailQueue
        {
            CreatedOn = _timeProvider.GetUtcNow(),
            Recipient = email,
            Subject = model.title,
            Body = message
        };

        return await _repository.Create(entity, cancellationToken);
    }

    public async Task<Result> EnqueueConfirmRegistration(string email, string name, string callbackUrl, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var model = new
        {
            title = "Registrierung bestätigen",
            name = name,
            link = callbackUrl,
            timeout = dc.Format(_confirmEmailTimeout),
            signature = _appSettings.Organizer
        };

        var renderer = new AccountEmailTemplateRenderer();
        var message = await renderer.Render(AccountEmailTemplate.ConfirmRegistration, model);

        var entity = new EmailQueue
        {
            CreatedOn = _timeProvider.GetUtcNow(),
            Recipient = email,
            Subject = model.title,
            Body = message
        };

        return await _repository.Create(entity, cancellationToken);
    }

    public async Task<Result> EnqueueResetPassword(string email, string name, string callbackUrl, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var model = new
        {
            title = "Passwort zurücksetzen",
            name = name,
            link = callbackUrl,
            timeout = dc.Format(_changeEmailOrPasswordTimeout),
            signature = _appSettings.Organizer
        };

        var renderer = new AccountEmailTemplateRenderer();
        var message = await renderer.Render(AccountEmailTemplate.ResetPassword, model);

        var entity = new EmailQueue
        {
            CreatedOn = _timeProvider.GetUtcNow(),
            Recipient = email,
            Subject = model.title,
            Body = message
        };

        return await _repository.Create(entity, cancellationToken);
    }

    public async Task<Result> EnqueueAcceptSeller(Domain.Models.BazaarEvent @event, string email, string name, CancellationToken cancellationToken)
    {
        var editEndDate = @event.EditArticleEndsOn ?? @event.StartsOn;
        var pickUpStart = @event.PickUpLabelsStartsOn ?? @event.StartsOn;
        var pickUpEnd = @event.PickUpLabelsEndsOn ?? @event.StartsOn;
        var dc = new GermanDateTimeConverter();

        var model = new
        {
            title = $"Registrierung zum {@event.Name}",
            name = name,
            eventname = @event.Name,
            date = dc.FormatFull(@event.StartsOn, @event.EndsOn),
            address = @event.Address,
            editenddate = dc.FormatFull(editEndDate, editEndDate),
            appurl = _appSettings.PublicUrl,
            signature = _appSettings.Organizer,
            pickupdate = dc.FormatFull(pickUpStart, pickUpEnd),
        };

        var renderer = new BazaarEmailTemplateRenderer();
        var message = await renderer.Render(BazaarEmailTemplate.AcceptSeller, model);
        var calendarEvent = new CalendarEvent().Create("Abholung der Etiketten", @event.Address, pickUpStart, pickUpEnd);

        var entity = new EmailQueue
        {
            CreatedOn = _timeProvider.GetUtcNow(),
            Recipient = email,
            Subject = model.title,
            Body = message,
            AttachmentBlob = Encoding.UTF8.GetBytes(calendarEvent),
            AttachmentName = "event.ics",
            AttachmentMimeType = CalendarEvent.MimeType
        };

        return await _repository.Create(entity, cancellationToken);
    }

    public async Task<Result> EnqueueDenySeller(Domain.Models.BazaarEvent @event, string email, string name, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var model = new
        {
            title = $"Registrierung zum {@event.Name}",
            name = name,
            eventname = @event.Name,
            date = dc.FormatFull(@event.StartsOn, @event.EndsOn),
            address = @event.Address,
            signature = _appSettings.Organizer,
        };

        var renderer = new BazaarEmailTemplateRenderer();
        var message = await renderer.Render(BazaarEmailTemplate.DenySeller, model);

        var entity = new EmailQueue
        {
            CreatedOn = _timeProvider.GetUtcNow(),
            Recipient = email,
            Subject = model.title,
            Body = message
        };

        return await _repository.Create(entity, cancellationToken);
    }
}
