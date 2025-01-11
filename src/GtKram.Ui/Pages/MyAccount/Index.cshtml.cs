using GtKram.Core.Repositories;
using GtKram.Core.User;
using GtKram.Ui.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.MyAccount;

[Node("Mein Konto", FromPage = typeof(Pages.IndexModel))]
[Authorize]
public class IndexModel : PageModel
{
    private readonly Core.Repositories.Users _users;
    private readonly TwoFactorAuth _twoFactorAuth;

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    public string? Email { get; set; }

    [BindProperty, Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    public bool IsDisabled { get; set; }
    public string? Info { get; set; }

    public IndexModel(Core.Repositories.Users users, TwoFactorAuth twoFactorAuth)
    {
        _users = users;
        _twoFactorAuth = twoFactorAuth;
    }

    public Task OnGetAsync(int? message, CancellationToken cancellationToken)
    {
        Info = message switch
        {
            1 => "Das Passwort wurde geändert.",
            2 => "Wir haben eine E-Mail an deine neue E-Mail-Adresse gesendet.",
            3 => "2FA wurde aktiviert. Bitte erneut anmelden!",
            4 => "2FA wurde deaktiviert.",
            _ => default
        };

        return Update(cancellationToken);
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await Update(cancellationToken)) return;

        var errors = await _users.Update(User.GetId(), Name!);
        if (errors != null)
        {
            errors.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e));
            return;
        }

        Info = "Änderungen wurden gespeichert.";
    }

    private async Task<bool> Update(CancellationToken cancellationToken)
    {
        var user = await _users.Find(User.GetId(), cancellationToken);
        if (user == null)
        {
            IsDisabled = true;
            return false;
        }

        Name = user.Name;
        Email = user.Email;

        return ModelState.IsValid;
    }
}
