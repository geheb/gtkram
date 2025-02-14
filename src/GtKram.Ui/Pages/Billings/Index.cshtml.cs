using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Billings;

[Node("Kasse", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    private readonly IBazaarEvents _bazaarEvents;
    public bool IsAdminOrManager { get; set; }
    public BazaarEventDto[] Items { get; private set; } = [];

    public IndexModel(IBazaarEvents bazaarEvents)
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
