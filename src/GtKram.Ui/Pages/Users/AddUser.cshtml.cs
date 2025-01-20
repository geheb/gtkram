using GtKram.Application.Repositories;
using GtKram.Application.UseCases.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

[Node("Benutzer anlegen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class AddUserModel : PageModel
{
    private readonly IUsers _users;

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public AddUserModel(IUsers users)
    {
        _users = users;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        if (Input.Roles.All(r => r == false))
        {
            ModelState.AddModelError(string.Empty, "Keine Rolle ausgewählt.");
            return Page();
        }

        var user = new UserDto();
        Input.To(user);

        var errors = await _users.Create(user,  cancellationToken);
        if (errors != null)
        {
            errors.ToList().ForEach(e => ModelState.AddModelError(string.Empty, e));
            return Page();
        }

        return RedirectToPage("Index");
    }
}
