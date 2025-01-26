using FluentResults;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Ui.Annotations;
using GtKram.Ui.Constants;
using GtKram.Ui.Extensions;
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

    public EditTwoFactorModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetOtpQuery(User.GetId()), cancellationToken);
        if (result2fa.IsFailed)
        {
            result2fa = await _mediator.Send(new CreateOtpCommand(User.GetId()), cancellationToken);
        }

        if (result2fa.IsSuccess)
        {
            IsTwoFactorEnabled = result2fa.Value.IsEnabled;
            SecretKey = result2fa.Value.SecretKey;
            AuthUri = result2fa.Value.AuthUri;
        }
        else
        {
            ModelState.AddError(result2fa.Errors);
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetOtpQuery(User.GetId()), cancellationToken);
        if (result2fa.IsFailed)
        {
            ModelState.AddError(result2fa.Errors);
            return Page();
        }

        IsTwoFactorEnabled = result2fa.Value.IsEnabled;
        SecretKey = result2fa.Value.SecretKey;
        AuthUri = result2fa.Value.AuthUri;

        Result result;
        if (IsTwoFactorEnabled)
        {
            result = await _mediator.Send(new DisableOtpCommand(User.GetId(), Code!), cancellationToken);
        }
        else
        {
            result = await _mediator.Send(new EnableOtpCommand(User.GetId(), Code!), cancellationToken);
        }

        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        if (!IsTwoFactorEnabled)
        {
            Response.Cookies.Delete(CookieNames.TwoFactorTrustToken);
        }

        return RedirectToPage("Index", new { message = IsTwoFactorEnabled ? 3 : 4 });
    }
}
