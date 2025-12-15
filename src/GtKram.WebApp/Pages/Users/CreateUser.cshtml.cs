using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Users;

[Node("Benutzer anlegen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class CreateUserModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public CreateUserModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var callbackUrl = Url.PageLink("/Login/ConfirmRegistration", values: new { id = Guid.Empty, token = string.Empty });

        var result = await _mediator.Send(Input.ToCommand(callbackUrl!), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
