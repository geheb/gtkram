using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Extensions;
using GtKram.Ui.I18n;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    [BindProperty]
    public RegisterSellerInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }
    public string? Message { get; set; }

    public RegisterModel(
        ILogger<RegisterModel> logger,
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, bool? success, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventQuery(id, true), cancellationToken);
        if (@event.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(@event.Errors);
            return;
        }

        var converter = new EventConverter();

        Input.State_Event = converter.Format(@event.Value);
        Input.State_Address = @event.Value.Address;

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
            _logger.LogWarning("Ungültige Anfrage von {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        var @event = await _mediator.Send(new FindEventQuery(id, true), cancellationToken);
        if (@event.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(@event.Errors);
            return Page();
        }

        var command = Input.ToCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = result.Errors!.Any(e => 
                e == Domain.Errors.Event.Expired ||
                e == Domain.Errors.EventRegistration.LimitExceeded);

            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage(string.Empty, new { id, success = true });       
    }
}
