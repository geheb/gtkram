using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Errors;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Artikel bearbeiten", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "seller,admin")]
public class EditArticleModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public bool IsDisabled { get; set; }

    [BindProperty]
    public ArticleInput Input { get; set; } = new();

    public EditArticleModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid sellerId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindSellerArticleByUserQuery(User.GetId(), id), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Input.State_Event = eventConverter.Format(result.Value.Event);
        Input.Init(result.Value.Article);

        if (eventConverter.IsExpired(result.Value.Event, _timeProvider) ||
            eventConverter.IsEditArticlesExpired(result.Value.Event, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, SellerArticle.EditExpired.Message);
        }

        if (result.Value.IsBooked)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, SellerArticle.EditFailedDueToBooked.Message);
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid sellerId, Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        if (!Input.HasPriceClosestToFifty)
        {
            ModelState.AddModelError(string.Empty, SellerArticle.InvalidPriceRange.Message);
            return Page();
        }

        var command = Input.ToUpdateCommand(User.GetId(), id);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Articles", new { sellerId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSellerArticleByUserCommand(User.GetId(), id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
