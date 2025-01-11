using GtKram.Core.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.Converter;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmChangePasswordModel : PageModel
{
    private readonly ILogger _logger;
    private readonly Core.Repositories.Users _users;

    [BindProperty]
    public string? UserName { get; set; } // just for bots

    [BindProperty, Display(Name = "Passwort")]
    [RequiredField, PasswordLengthField]
    public string? Password { get; set; }

    [BindProperty, Display(Name = "Passwort wiederholen")]
    [RequiredField, PasswordLengthField]
    [CompareField(nameof(Password))]
    public string? RepeatPassword { get; set; }

    public bool IsDisabled { get; set; }
    public string ChangePasswordEmail { get; set; } = "n.v.";

    public ConfirmChangePasswordModel(
        ILogger<ConfirmChangePasswordModel> logger,
        Core.Repositories.Users users)
    {
        _logger = logger;
        _users = users;
    }

    public async Task OnGetAsync(Guid id, string token)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        var email = await _users.VerfiyChangePassword(id, token);
        if (string.IsNullOrEmpty(email))
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidPasswordResetLink);
            return;
        }

        ChangePasswordEmail = new EmailConverter().Anonymize(email);           
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string token)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(token) || !string.IsNullOrEmpty(UserName))
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        var result = await _users.ChangePassword(id, token, Password!);
        if (string.IsNullOrEmpty(result.Email))
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidPasswordResetLink);
            return Page();
        }
        else
        {
            ChangePasswordEmail = new EmailConverter().Anonymize(result.Email);
        }

        if (result.Error != null)
        {
            result.Error.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e));
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }
}
