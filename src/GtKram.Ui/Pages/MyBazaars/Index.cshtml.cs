using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Meine Teilnahme", FromPage = typeof(Pages.IndexModel))]
[Authorize(Roles = "seller,admin")]
public class IndexModel : PageModel
{
    private readonly BazaarSellers _bazaarSellers;

    public BazaarSellerDto[] Events { get; private set; } = [];

    public IndexModel(BazaarSellers bazaarSellers)
    {
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await _bazaarSellers.GetAll(User.GetId(), cancellationToken);
    }
}
