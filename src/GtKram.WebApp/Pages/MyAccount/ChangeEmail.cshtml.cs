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

[Node("E-Mail-Adresse ändern", FromPage = typeof(IndexModel))]
[Authorize]
public class ChangeEmailModel : PageModel
{
    private readonly IMediator _mediator;

    [Display(Name = "Aktuelle E-Mail-Adresse")]
    public string? CurrentEmail { get; set; }

    [BindProperty, Display(Name = "Neue E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? NewEmail { get; set; }

    [BindProperty, Display(Name = "Aktuelles Passwort")]
    [RequiredField, PasswordLengthField(MinimumLength = 8)]
    public string? CurrentPassword { get; set; }

    public ChangeEmailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet()
    {
        CurrentEmail = User.GetEmail();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        CurrentEmail = User.GetEmail();

        if (!ModelState.IsValid) return Page();

        var callbackUrl = Url.PageLink("/Login/ConfirmChangeEmail", values: new { id = User.GetId(), token = string.Empty, email = NewEmail });

        var result = await _mediator.Send(new SendChangeEmailCommand(User.GetId(), NewEmail!, CurrentPassword!, callbackUrl!), cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }       

        return RedirectToPage("Index", new { message = 2 });
    }
}
