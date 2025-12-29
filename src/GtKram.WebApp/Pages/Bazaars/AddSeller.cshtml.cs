using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Errors;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Verkäufer anlegen", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public sealed class AddSellerModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    [BindProperty]
    public AddSellerInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public AddSellerModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (result.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var converter = new EventConverter();
        Input.State_Event = converter.Format(result.Value);

        if (converter.IsExpired(result.Value, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddError(Event.Expired);
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToCommand(eventId), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Sellers", new { eventId });
    }
}
