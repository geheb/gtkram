using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Checkouts;

[Node("Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public sealed class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public EventWithCheckoutTotals[] Items { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventWithCheckoutTotalsQuery(), cancellationToken);
    }
}
