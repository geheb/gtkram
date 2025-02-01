using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Converter;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verkäufer anlegen", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public class AddSellerModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public AddSellerInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public AddSellerModel(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        Input.Event = new EventConverter().Format(result.Value);
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToCommand(eventId), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Sellers", new { eventId });
    }
}
