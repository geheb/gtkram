using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Users;

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

        var result = await _mediator.Send(Input.ToCommand(), cancellationToken);
        if (result.IsFailed)
        {
            result.Errors.ForEach(e => ModelState.AddModelError(string.Empty, e.Message));
            return Page();
        }

        return RedirectToPage("Index");
    }
}
