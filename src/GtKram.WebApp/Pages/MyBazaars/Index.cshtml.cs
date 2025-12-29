using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.MyBazaars;

[Node("Meine Kinderbasare", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "seller")]
public sealed class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public EventWithSellerAndArticleCount[] Items { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventsWithSellerAndArticleCountByUserQuery(User.GetId()), cancellationToken);
    }
}
