using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

[Node("Benutzer bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class EditUserModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly ITwoFactorAuth _twoFactorAuth;

    [BindProperty]
    public UpdateUserInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public EditUserModel(
        IMediator mediator, 
        ITwoFactorAuth twoFactorAuth)
    {
        _mediator = mediator;
        _twoFactorAuth = twoFactorAuth;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return;
        }

        Input.FromDomain(result.Value);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        result = await _mediator.Send(Input.ToCommand(id), cancellationToken);
        if (result.IsFailed)
        {
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        return RedirectToPage("Index");
    }
    public IActionResult OnPostConfirmEmail(Guid id)
    {
        var result = false;// await _users.NotifyConfirmRegistration(id, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostResetTwoFactorAsync(Guid id)
    {
        var result = await _twoFactorAuth.Reset(id);
        return new JsonResult(result);
    }
}
