using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Verwaltung", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public EventWithRegistrationCount[] Items { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventsWithRegistrationCountQuery(), cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEventCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
