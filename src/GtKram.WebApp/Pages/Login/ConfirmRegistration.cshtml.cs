using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Infrastructure.AspNetCore.Annotations;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.WebApp.Converter;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.Login;

[AllowAnonymous]
public sealed class ConfirmRegistrationModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
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
        IMediator mediator)
    {
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
        if (result.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Identity.LinkIsInvalidOrExpired);
            return;
        }

        var resultUser = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (resultUser.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Identity.LinkIsInvalidOrExpired);
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
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        var result = await _mediator.Send(new ConfirmRegistrationCommand(id, Password!, token), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }
}
