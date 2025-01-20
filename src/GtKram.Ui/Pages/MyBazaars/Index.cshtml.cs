using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Meine Teilnahme", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "seller,admin")]
public class IndexModel : PageModel
{
    private readonly IBazaarSellers _bazaarSellers;

    public BazaarSellerDto[] Events { get; private set; } = [];

    public IndexModel(IBazaarSellers bazaarSellers)
    {
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _bazaarSellers.GetAll(User.GetId(), cancellationToken);
    }
}
