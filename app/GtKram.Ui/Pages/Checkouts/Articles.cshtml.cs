using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace GtKram.Ui.Pages.Checkouts;

[Node("Arikel", FromPage = typeof(CheckoutModel))]
[Authorize(Roles = "manager,admin")]
public class ArticlesModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; private set; } = "Unbekannt";
    public ArticleWithCheckout[] Items { get; private set; } = [];
    public bool CanComplete { get; private set; }

    public ArticlesModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetArticlesWithCheckoutAndEventQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Event = eventConverter.Format(result.Value.Event);
        Items = result.Value.Articles;

        if (eventConverter.IsExpired(result.Value.Event, _timeProvider))
        {
            ModelState.AddError(Domain.Errors.Event.Expired);
        }
        
        CanComplete = Items.Length > 0 && !result.Value.Checkout.IsCompleted;
    }

    public async Task<IActionResult> OnPostSumAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindCheckoutTotalQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            return new JsonResult(null);
        }
        return new JsonResult(new { count = result.Value.ArticleCount, total = result.Value.Total.ToString("0.00", CultureInfo.InvariantCulture) });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid articleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCheckoutArticleCommand(id, articleId), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelCheckoutCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteCheckoutCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
