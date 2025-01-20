using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verwaltung", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public class IndexModel : PageModel
{
    private readonly IBazaarEvents _bazaarEvents;

    public BazaarEventDto[] Events { get; private set; } = [];

    public IndexModel(IBazaarEvents bazaarEvents)
    {
        _bazaarEvents = bazaarEvents;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _bazaarEvents.GetAll(cancellationToken);
    }
}
