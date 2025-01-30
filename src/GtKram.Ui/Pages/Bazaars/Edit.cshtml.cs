using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    public bool IsDisabled { get; set; }

    [BindProperty]
    public BazaarEventInput Input { get; set; } = new();

    public EditModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindBazaarEventQuery(id), cancellationToken);
        if (result.IsFailed)
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

        var result = await _mediator.Send(Input.ToCommand(), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteBazaarEventCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
