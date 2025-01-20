using GtKram.Application.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.Converter;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmRegistrationModel : PageModel
{
    private readonly IUsers _users;
    private readonly ILogger _logger;

    public string ConfirmedEmail { get; set; } = "n.v.";
    public bool IsDisabled { get; set; }

    [BindProperty, Display(Name = "Passwort")]
    [RequiredField, PasswordLengthField]
    public string? Password { get; set; }

    [BindProperty, Display(Name = "Passwort wiederholen")]
    [RequiredField, PasswordLengthField]
    [CompareField(nameof(Password))]
    public string? RepeatPassword { get; set; }

    public ConfirmRegistrationModel(IUsers users, ILogger<ConfirmRegistrationModel> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task OnGetAsync(Guid id, string token)
    {
        await Verify(id, token);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string token)
    {
        if (!await Verify(id, token) || !ModelState.IsValid)
        {
            return Page();
        }

        var error = await _users.ConfirmRegistrationAndSetPassword(id, token, Password!);
        if (error != null)
        {
            error.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e));
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }

    private async Task<bool> Verify(Guid id, string token)
    {
        if (id == Guid.Empty || string.IsNullOrEmpty(token))
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return false;
        }

        var email = await _users.VerifyConfirmRegistration(id, token);
        if (string.IsNullOrEmpty(email))
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidAccountConfirmationLink);
            return false;
        }

        ConfirmedEmail = new EmailConverter().Anonymize(email);
        return true;
    }
}
