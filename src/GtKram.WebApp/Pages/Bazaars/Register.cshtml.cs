using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Errors;
using GtKram.Infrastructure.AspNetCore.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[AllowAnonymous]
public sealed class RegisterModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    [BindProperty]
    public RegisterSellerInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }
    public string? Message { get; set; }

    public RegisterModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, bool? success, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventForRegistrationQuery(id), cancellationToken);
        if (@event.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(@event.Errors);
            return;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value.Event, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddError(Event.Expired);
        }

        if (@event.Value.Event.HasRegistrationsLocked)
        {
            IsDisabled = true;
            ModelState.AddError(SellerRegistration.IsLocked);
        }
        else if (!converter.IsRegisterExpired(@event.Value.Event, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddError(SellerRegistration.IsExpired);
        }
        else if(@event.Value.RegistrationCount >= @event.Value.Event.MaxSellers)
        {
            IsDisabled = true;
            ModelState.AddError(SellerRegistration.LimitExceeded);
        }

        Input.State_Event = converter.Format(@event.Value.Event);
        Input.State_Address = @event.Value.Event.Address;

        if (success == true)
        {
            IsDisabled = true;
            Message = "Vielen Dank für die unverbindliche Registrierung. Du erhältst bald eine Zu- oder Absage per E-Mail.";
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(Input.SellerUserName))
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        var command = Input.ToCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsError)
        {
            IsDisabled = result.Errors.Any(e => 
                e.Code == Domain.Errors.Event.Expired.Code ||
                e.Code == Domain.Errors.SellerRegistration.LimitExceeded.Code);

            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage(string.Empty, new { id, success = true });       
    }
}
