using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Authorize(Roles = "manager,admin")]
public class PrintSellerArticlesModel : PageModel
{
    private readonly IMediator _mediator;

    public int SellerNumber { get; set; }
    public BazaarSellerArticleWithBilling[] Items { get; private set; } = [];

    public PrintSellerArticlesModel(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindSellerWithRegistrationAndArticlesQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        Items = result.Value.Articles;
        SellerNumber = result.Value.Seller.SellerNumber;
    }
}
