using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verwaltung", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "manager,admin")]
public class IndexModel : PageModel
{
    private readonly BazaarEvents _bazaarEvents;

    public BazaarEventDto[] Events { get; private set; } = [];

    public IndexModel(BazaarEvents bazaarEvents)
    {
        _bazaarEvents = bazaarEvents;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _bazaarEvents.GetAll(cancellationToken);
    }
}
