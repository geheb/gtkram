using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Ui.Annotations;
using GtKram.Ui.Converter;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmRegistrationModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public string ConfirmedEmail { get; set; } = "n.v.";
    public bool IsDisabled { get; set; }

    [BindProperty, Display(Name = "Passwort")]
    [RequiredField, PasswordLengthField]
    public string? Password { get; set; }

    [BindProperty, Display(Name = "Passwort wiederholen")]
    [RequiredField, PasswordLengthField]
    [CompareField(nameof(Password))]
    public string? RepeatPassword { get; set; }

    public ConfirmRegistrationModel(
        ILogger<ConfirmRegistrationModel> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, string token, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || string.IsNullOrEmpty(token))
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return;
        }

        var result = await _mediator.Send(new VerifyConfirmRegistrationQuery(id, token), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Identity.LinkIsExpired);
            return;
        }

        var resultUser = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (resultUser.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return;
        }

        ConfirmedEmail = new EmailConverter().Anonymize(resultUser.Value.Email);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string token, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (id == Guid.Empty || string.IsNullOrEmpty(token))
        {
            IsDisabled = true;
            _logger.LogWarning("Ungültige Anfrage von {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        var resultUser = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (resultUser.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        var result = await _mediator.Send(new ConfirmRegistrationCommand(id, Password!, token), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(Domain.Errors.Identity.LinkIsExpired);
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }
}
