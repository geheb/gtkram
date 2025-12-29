using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Infrastructure.AspNetCore.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Authorize(Roles = "manager,admin")]
public sealed class PrintSellerArticlesModel : PageModel
{
    private readonly IMediator _mediator;

    public ArticleWithCheckout[] Items { get; private set; } = [];

    public PrintSellerArticlesModel(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindSellerWithRegistrationAndArticlesQuery(id), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        Items = result.Value.Articles;
    }
}
