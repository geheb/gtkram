using GtKram.Application.Services;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmCodeModel : PageModel
{
    private readonly ILogger _logger;
    private readonly ITwoFactorAuth _twoFactorAuth;

    [BindProperty]
    public string? UserName { get; set; }

    [BindProperty, Display(Name = "Bestätigungscode aus der App")]
    [RequiredField, TextLengthField(6, MinimumLength = 6)]
    public string? Code { get; set; }

    [BindProperty, Display(Name = "Diesen Browser vertrauen")]
    public bool IsTrustBrowser { get; set; }

    public string? ReturnUrl { get; set; }
    public bool IsDisabled { get; set; }

    public ConfirmCodeModel(
        ILogger<ConfirmCodeModel> logger,
        ITwoFactorAuth twoFactorAuth)
    {
        _logger = logger;
        _twoFactorAuth = twoFactorAuth;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return LocalRedirect("/");
        }

        if (await Update())
        {
            ReturnUrl = returnUrl;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!await Update()) return Page();

        var result = await _twoFactorAuth.SignIn(Code!, IsTrustBrowser);
        if (result.Succeeded)
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
            ModelState.AddModelError(string.Empty, "Code ist ungültig");
        }
        return Page();
    }

    public async Task<bool> Update()
    {
        if (!string.IsNullOrEmpty(UserName))
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return false;
        }

        if (!await _twoFactorAuth.HasUserAuthentication())
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Die Anmeldung is abgelaufen, bitte zurück zum Login und erneut versuchen.");
            return false;
        }

        return ModelState.IsValid;
    }
}
