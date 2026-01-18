using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Plannings;

[Node("Planungen", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public sealed class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public EventWithPlanningCount[] Items { get; private set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetEventsWithPlanningCountQuery(), cancellationToken);
    }
}

/*
 *  Freitag: 
 *      Aufbau: 9-13 Uhr
 *  Samstag:
 *      Kasse 1: 13-17 Uhr
 *      Kasse 2: 13-16 Uhr
 *      Kasse 3: 13-17 Uhr
 *      Kasse 4: 13-17 Uhr
 *      Aufsicht gemeinde Saal: 13-15 Uhr 
 *      Aufsicht gemeinde Saal: 15-17 Uhr
 *      Kinderwagen- und Taschengarderobe: 12-15 Uhr
 *      Kinderwagen- und Taschengarderobe: 15-17 Uhr
 *      Küche & Cafeteria: 13-15 Uhr
 *      Küche & Cafeteria: 15-17 Uhr
 *      Bücherraum: 13-15 Uhr
 *      Bücherraum: 15-17 Uhr
 *      Cafeteria (Ausgabe): 13-17 Uhr
 *      Aufsicht Turnr.: 13-15 Uhr
 *      Aufsicht Turnr.: 15-17 Uhr
 *      Eimerdienst: 13-15 Uhr
 *      Eimerdienst: 15-17 Uhr
 *      Glücksrad: 13-15 Uhr
 *      Glücksrad: 15-17 Uhr
 *      Einlass: 13-17 Uhr
 *      Waffeln backen: 13-15 Uhr
 *      Waffeln backen: 15-17 Uhr
 *      Abbau leicht: 17-21 Uhr
 *      Abbauch Tische: 17-21 Uhr
 *  Sonntag:
 *      Aufräumen/Abholung: 12-13:30 Uhr
 */