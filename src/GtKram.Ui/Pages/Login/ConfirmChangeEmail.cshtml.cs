using GtKram.Application.Repositories;
using GtKram.Ui.Converter;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmChangeEmailModel : PageModel
{
    private readonly IUsers _users;
    private readonly ILogger _logger;

    public string ConfirmedEmail { get; set; } = "n.v.";

    public ConfirmChangeEmailModel(IUsers users, ILogger<ConfirmChangeEmailModel> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task OnGetAsync(Guid id, string token, string email)
    {
        if (id == Guid.Empty || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        var newEmail = await _users.ConfirmChangeEmail(id, token, email);
        if (string.IsNullOrEmpty(newEmail))
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidNewEmailConfirmationLink);
            return;
        }

        ConfirmedEmail = new EmailConverter().Anonymize(newEmail);
    }
}
