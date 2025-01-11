using GtKram.Core.Repositories;
using GtKram.Core.User;
using GtKram.Ui.Annotations;
using GtKram.Ui.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.MyAccount;

[Node("2FA bearbeiten", FromPage = typeof(IndexModel))]
[Authorize]
public class EditTwoFactorModel : PageModel
{
    private readonly TwoFactorAuth _twoFactorAuth;

    [BindProperty, Display(Name = "6-stelliger Code aus der Authenticator-App")]
    [RequiredField, TextLengthField(6, MinimumLength = 6)]
    public string? Code { get; set; }

    [Display(Name = "Geheimer Schlüssel")]
    public string? SecretKey { get; set; }
    public string? AuthUri { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public bool IsDisabled { get; set; }

    public EditTwoFactorModel(TwoFactorAuth twoFactorAuth)
    {
        _twoFactorAuth = twoFactorAuth;
    }

    public Task OnGetAsync()
    {
        return Update();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await Update()) return Page();

        var enable = !IsTwoFactorEnabled;

        var result = await _twoFactorAuth.Enable(User.GetId(), enable, Code!);
        if (result.IsFailed)
        {
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        if (!enable)
        {
            Response.Cookies.Delete(CookieNames.TwoFactorTrustToken);
        }

        return RedirectToPage("Index", new { message = enable ? 3 : 4 });
    }

    private async Task<bool> Update()
    {
        var result = await _twoFactorAuth.GenerateKey(User.GetId());
        if (result.IsFailed)
        {
            IsDisabled = true;
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return false;
        }

        SecretKey = result.Value.SecretKey;
        AuthUri = result.Value.AuthUri;
        IsTwoFactorEnabled = result.Value.IsEnabled;
        return ModelState.IsValid;
    }
}
