using FluentResults;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Ui.Annotations;
using GtKram.Ui.Constants;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.MyAccount;

[Node("2FA bearbeiten", FromPage = typeof(IndexModel))]
[Authorize]
public class EditTwoFactorModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty, Display(Name = "6-stelliger Code aus der Authenticator-App")]
    [RequiredField, TextLengthField(6, MinimumLength = 6)]
    public string? Code { get; set; }

    [Display(Name = "Geheimer Schlüssel")]
    public string? SecretKey { get; set; }

    public string? AuthUri { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public bool IsDisabled { get; set; }

    public EditTwoFactorModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetTwoFactorAuthQuery(User.GetId()), cancellationToken);
        if (result2fa.IsFailed)
        {
            result2fa = await _mediator.Send(new CreateTwoFactorAuthCommand(User.GetId()), cancellationToken);
        }

        if (result2fa.IsSuccess)
        {
            IsTwoFactorEnabled = result2fa.Value.IsEnabled;
            SecretKey = result2fa.Value.SecretKey;
            AuthUri = result2fa.Value.AuthUri;
        }
        else
        {
            IsDisabled = true;
            result2fa.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetTwoFactorAuthQuery(User.GetId()), cancellationToken);
        if (result2fa.IsFailed)
        {
            IsDisabled = true;
            result2fa.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        IsTwoFactorEnabled = result2fa.Value.IsEnabled;
        SecretKey = result2fa.Value.SecretKey;
        AuthUri = result2fa.Value.AuthUri;

        Result result;
        if (IsTwoFactorEnabled)
        {
            result = await _mediator.Send(new DisableTwoFactorAuthCommand(User.GetId(), Code!), cancellationToken);
        }
        else
        {
            result = await _mediator.Send(new EnableTwoFactorAuthCommand(User.GetId(), Code!), cancellationToken);
        }

        if (result.IsFailed)
        {
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        if (!IsTwoFactorEnabled)
        {
            Response.Cookies.Delete(CookieNames.TwoFactorTrustToken);
        }

        return RedirectToPage("Index", new { message = IsTwoFactorEnabled ? 3 : 4 });
    }
}
