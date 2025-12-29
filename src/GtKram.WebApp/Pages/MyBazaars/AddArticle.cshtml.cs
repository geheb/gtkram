using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Errors;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.MyBazaars;

[Node("Anlegen", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "seller")]
public sealed class AddArticleModel : PageModel
{
    private readonly IMediator _mediator;

    public bool IsDisabled { get; set; }

    [BindProperty]
    public ArticleInput Input { get; set; } = new();

    public AddArticleModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindSellerEventByUserQuery(User.GetId(), sellerId), cancellationToken);
        if (result.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Input.State_Event = eventConverter.Format(result.Value);
    }

    public async Task<IActionResult> OnPostAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        if (!Input.HasPriceClosestToFifty)
        {
            ModelState.AddError(SellerArticle.InvalidPriceRange);
            return Page();
        }

        var command = Input.ToCreateCommand(User.GetId(), sellerId);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Articles", new { sellerId });
    }
}
