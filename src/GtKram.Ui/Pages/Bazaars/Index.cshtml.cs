using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verwaltung", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public BazaarEventWithRegistrationCount[] Items { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventsWithRegistrationCountQuery(), cancellationToken);
    }
}
