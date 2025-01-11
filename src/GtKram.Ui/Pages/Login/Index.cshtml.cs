using GtKram.Core.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly ILogger _logger;
    private readonly EmailAuth _emailAuth;

    [BindProperty]
    public string? UserName { get; set; }

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [BindProperty, Display(Name = "Passwort")]
    [RequiredField, PasswordLengthField(MinimumLength = 8)]
    public string? Password { get; set; }

    public string? Message { get; set; }

    public IndexModel(ILogger<IndexModel> logger, EmailAuth emailAuth)
    {
        _logger = logger;
        _emailAuth = emailAuth;
    }

    public void OnGet(int message = 0)
    {
        if (message == 1)
        {
            Message = "Das Passwort wurde geändert. Melde dich jetzt mit dem neuen Passwort an.";
        }
        else if (message == 2)
        {
            Message = "Eine E-Mail wird an die E-Mail-Adresse versendet, um das Passwort zu ändern.";
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(UserName))
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        var result = await _emailAuth.SignIn(Email!, Password!);
        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("ConfirmCode", new { returnUrl });
        }
        else if (result.Succeeded)
        {
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }
        else if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Login ist gesperrt");
        }
        else if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Login ist nicht erlaubt");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email/Passsort stimmen nicht überein");
        }

        return Page();
    }
}
