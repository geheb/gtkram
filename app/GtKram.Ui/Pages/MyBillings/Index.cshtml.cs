using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBillings;

[Node("Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public BazaarEventWithBillingCount[] Items { get; private set; } = [];
    
    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventsWithBillingByUserQuery(User.GetId()), cancellationToken);
    }
}
