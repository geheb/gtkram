using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verwaltung", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public BazaarEventWithRegistrationCount[] Events { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _mediator.Send(new GetEventsWithRegistrationCountQuery(), cancellationToken);
    }
}
