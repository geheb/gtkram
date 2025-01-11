using GtKram.Core.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

[Node("Benutzer", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class IndexModel : PageModel
{
    private readonly Core.Repositories.Users _users;
    public UserDto[] Users { get; private set; } = [];
    public int UsersConfirmed { get; set; }
    public int UsersNotConfirmed { get; set; }
    public int UsersLocked { get; set; }

    public IndexModel(Core.Repositories.Users users)
    {
        _users = users;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await _users.GetAll(cancellationToken);
        UsersConfirmed = Users.Count(u => u.IsEmailConfirmed);
        UsersNotConfirmed = Users.Count(u => !u.IsEmailConfirmed);
        UsersLocked = Users.Count(u => u.IsLockedUntil.HasValue);
    }
}
