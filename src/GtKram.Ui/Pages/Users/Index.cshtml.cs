using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

[Node("Benutzer", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public User[] Items { get; private set; } = [];
    public int UsersConfirmed { get; set; }
    public int UsersNotConfirmed { get; set; }
    public int UsersLocked { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
        UsersConfirmed = Items.Count(u => u.IsEmailConfirmed);
        UsersNotConfirmed = Items.Count(u => !u.IsEmailConfirmed);
        UsersLocked = Items.Count(u => u.LockoutEndDate.HasValue);
    }
}
