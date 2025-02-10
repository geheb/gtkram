using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Models;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Artikel", FromPage = typeof(IndexModel))]
[Authorize(Roles = "seller,admin")]
public class ArticlesModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string? Event { get; set; }
    public string? EditArticleEndDate { get; set; }
    public int MaxArticleCount { get; set; }
    public bool CanEdit { get; set; }
    public bool CanAdd { get; set; }
    public bool IsRegisterAccepted { get; set; }
    public int AvailableCount { get; set; }
    public decimal AvailableTotalValue { get; set; }
    public int SoldCount { get; set; }
    public decimal SoldTotalValue { get; set; }
    public decimal PayoutTotalValue { get; set; }
    public BazaarSellerArticle[] Items { get; set; } = [];

    public ArticlesModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindSellerWithEventAndArticlesQuery(sellerId, User.GetId()), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        var @event = result.Value.Event;
        
        Items = result.Value.Articles;
        Event = eventConverter.Format(@event);
        MaxArticleCount = result.Value.Seller.MaxArticleCount;
        EditArticleEndDate = @event.EditArticleEndsOn is not null 
            ? new GermanDateTimeConverter().ToDateTime(@event.EditArticleEndsOn.Value)
            : null;
        CanEdit = 
            !eventConverter.IsExpired(@event, _timeProvider) &&
            !eventConverter.IsEditArticlesExpired(@event, _timeProvider);
        CanAdd = CanEdit && Items.Length < MaxArticleCount;

        if (!CanEdit)
        {
            ModelState.AddModelError(string.Empty, "Die Bearbeitung der Artikel ist abgelaufen.");
        }

        AvailableCount = Items.Length;
        AvailableTotalValue = Items.Sum(a => a.Price);
        var sold = Items.Where(a => a.IsSold);
        SoldCount = sold.Count();
        SoldTotalValue = sold.Sum(a => a.Price);
        PayoutTotalValue = eventConverter.CalcPayout(@event, SoldTotalValue);
    }

    public async Task<IActionResult> OnPostTakeOverArticlesAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TakeOverSellerArticlesCommand(sellerId, User.GetId()), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
