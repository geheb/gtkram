using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<IActionResult> OnPostConfirmEmail(Guid id, CancellationToken cancellationToken)
    {
        var callbackUrl = Url.PageLink("/Login/ConfirmRegistration", values: new { id, token = string.Empty });
        var result = await _mediator.Send(new SendConfirmRegistrationCommand(id, callbackUrl!), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostResetTwoFactorAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetOtpCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
