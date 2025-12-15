using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.WebApp.Annotations;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.MyAccount;

[Node("Passwort ändern", FromPage = typeof(IndexModel))]
[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty, Display(Name = "Aktuelles Passwort")]
    [RequiredField, PasswordLengthField(MinimumLength = 8)]
    public string? CurrentPassword { get; set; }

    [BindProperty, Display(Name = "Neues Passwort")]
    [RequiredField, PasswordLengthField]
    public string? NewPassword { get; set; }

    [BindProperty, Display(Name = "Neues Passwort bestätigen")]
    [RequiredField, PasswordLengthField]
    [CompareField(nameof(NewPassword))]
    public string? ConfirmNewPassword { get; set; }

    public ChangePasswordModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(new ChangePasswordCommand(User.GetId(), CurrentPassword!, NewPassword!), cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }
}
