using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Billings;

[Node("Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "billing,admin")]
public class IndexModel : PageModel
{
    private readonly BazaarEvents _bazaarEvents;
    public bool IsAdminOrManager { get; set; }
    public BazaarEventDto[] Items { get; private set; } = [];

    public IndexModel(BazaarEvents bazaarEvents)
    {
        _bazaarEvents = bazaarEvents;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IsAdminOrManager = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
        if (IsAdminOrManager)
        {
            Items = await _bazaarEvents.GetAll(cancellationToken);
        }
        else
        {
            Items = await _bazaarEvents.GetAll(User.GetId(), cancellationToken);
        }
    }
}
