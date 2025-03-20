using GtKram.Application.UseCases.User.Commands;
using GtKram.Ui.Annotations;
using GtKram.Ui.Extensions;
using GtKram.Ui.I18n;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    [BindProperty]
    public string? UserName { get; set; } // just for Bots

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [BindProperty, Display(Name = "Passwort")]
    [RequiredField, PasswordLengthField(MinimumLength = 8)]
    public string? Password { get; set; }

    public string? Message { get; set; }

    public bool IsDisabled { get; set; }

    public IndexModel(
        ILogger<IndexModel> logger, 
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public void OnGet(int message = 0)
    {
        if (message == 1)
        {
            Message = "Das Passwort wurde geändert. Melde dich jetzt mit dem neuen Passwort an.";
        }
        else if (message == 2)
        {
            Message = "Um das Passwort zu ändern, wurde eine E-Mail versendet.";
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(UserName))
        {
            IsDisabled = true;
            _logger.LogWarning("Ungültige Anfrage von {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _mediator.Send(new SignInCommand(Email!, Password!), cancellationToken);

        if (result.IsSuccess)
        {
            if (result.Value.Requires2FA)
            {
                return RedirectToPage("ConfirmCode", new { returnUrl });
            }
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }

        ModelState.AddError(Domain.Errors.Identity.LoginFailed);
        return Page();
    }
}
