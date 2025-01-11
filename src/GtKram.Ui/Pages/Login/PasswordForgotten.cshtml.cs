using GtKram.Core.Email;
using GtKram.Core.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class PasswordForgottenModel : PageModel
{
    private readonly ILogger _logger;
    private readonly Core.Repositories.Users _users;
    private readonly EmailValidatorService _emailValidator;

    [BindProperty]
    public string? UserName { get; set; } // just for Bots

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [BindProperty]
    public bool IsDisabled { get; set; }

    public PasswordForgottenModel(
        ILogger<PasswordForgottenModel> logger,
        Core.Repositories.Users users,
        EmailValidatorService emailValidator)
    {
        _logger = logger;
        _users = users;
        _emailValidator = emailValidator;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(UserName)) 
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        if (!await _emailValidator.Validate(Email!, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidEmail);
            return Page();
        }

        await _users.NotifyPasswordForgotten(Email!, cancellationToken);

        return RedirectToPage("Index", new { message = 2 });
    }
}
