using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.MyCheckouts;

[Node("Meine Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "checkout")]
public sealed class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public EventWithCheckoutCount[] Items { get; private set; } = [];
    
    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventWithCheckoutCountByUserQuery(User.GetId()), cancellationToken);
    }
}
