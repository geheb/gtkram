using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Errors;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace GtKram.WebApp.Pages.MyCheckouts;

[Node("Kassenartikel", FromPage = typeof(CheckoutModel))]
[Authorize(Roles = "checkout")]
public class ArticlesModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; private set; } = "Unbekannt";
    public ArticleWithCheckout[] Items { get; private set; } = [];
    public bool CanEdit { get; private set; }
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
        var result = await _mediator.Send(new GetArticlesWithCheckoutAndEventByUserQuery(User.GetId(), id), cancellationToken);
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
        else
        {
            CanEdit = !result.Value.Checkout.IsCompleted;
            CanComplete = Items.Length > 0 && !result.Value.Checkout.IsCompleted;
        }
    }

    public async Task<IActionResult> OnPostAddAsync(Guid id, Guid articleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCheckoutArticleByUserCommand(User.GetId(), id, articleId), cancellationToken);
        if (result.IsSuccess)
        {
            return new JsonResult(new { created = true });
        }

        if (result.Errors.Any(e => e == Checkout.AlreadyBooked))
        {
            return new JsonResult(new { exists = true });
        }

        if (result.Errors.Any(e => e == SellerArticle.NotFound))
        {
            return new JsonResult(new { notfound = true });
        }

        return new JsonResult(null);
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
        var result = await _mediator.Send(new DeleteCheckoutArticleByUserCommand(User.GetId(), id, articleId), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelCheckoutByUserCommand(User.GetId(), id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteCheckoutByUserCommand(User.GetId(), id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
