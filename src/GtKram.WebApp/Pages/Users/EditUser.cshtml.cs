using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Users;

[Node("Benutzer bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public sealed class EditUserModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public UpdateUserInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public EditUserModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (result.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        Input.Init(result.Value);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToCommand(id), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        result = await _mediator.Send(new UpdateAuthCommand(id, Input.Email, Input.Password), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostRemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveUserCommand(id), cancellationToken);
        return new JsonResult(!result.IsError);
    }
}
