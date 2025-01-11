using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

[Node("Benutzer bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class EditUserModel : PageModel
{
    private readonly Core.Repositories.Users _users;
    private readonly TwoFactorAuth _twoFactorAuth;

    public Guid? Id { get; set; }

    [BindProperty]
    public UpdateUserInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public EditUserModel(Core.Repositories.Users users, TwoFactorAuth twoFactorAuth)
    {
        _users = users;
        _twoFactorAuth = twoFactorAuth;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Id = id;
        if (!ModelState.IsValid) return;

        var user = await _users.Find(id, cancellationToken);
        if (user == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Benutzer wurde nicht gefunden.");
            return;
        }

        Input = new UpdateUserInput();
        Input.From(user);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        Id = id;
        if (!ModelState.IsValid) return Page();

        if (Input.Roles.All(r => r == false))
        {
            ModelState.AddModelError(string.Empty, "Keine Rolle ausgewählt.");
            return Page();
        }

        var user = await _users.Find(id, cancellationToken);
        if (user == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Benutzer wurde nicht gefunden.");
            return Page();
        }

        Input.To(user);

        var errors = await _users.Update(user, Input.Password!, cancellationToken);
        if (errors != null)
        {
            errors.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e));
            return Page();
        }

        return RedirectToPage("Users");
    }
    public async Task<IActionResult> OnPostConfirmEmailAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.NotifyConfirmRegistration(id, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostResetTwoFactorAsync(Guid id)
    {
        var result = await _twoFactorAuth.Reset(id);
        return new JsonResult(result);
    }
}
