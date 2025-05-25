using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Artikel", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public class SellerArticlesModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; set; } = "Unbekannt";
    public string? SellerName { get; set; }
    public int SellerNumber { get; set; }
    public int MaxArticleCount { get; set; }
    public int AvailableCount { get; set; }
    public decimal AvailableTotalValue { get; set; }
    public int SoldCount { get; set; }
    public decimal SoldTotalValue { get; set; }
    public int Commission { get; set; }
    public decimal PayoutTotalValue { get; set; }
    public ArticleWithCheckout[] Items { get; private set; } = [];

    public SellerArticlesModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (@event.IsFailed)
        {
            ModelState.AddError(@event.Errors);
            return;
        }

        var converter = new EventConverter();
        Event = converter.Format(@event.Value);
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            ModelState.AddError(Domain.Errors.Event.Expired);
        }

        var result = await _mediator.Send(new FindSellerWithRegistrationAndArticlesQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        SellerNumber = result.Value.Seller.SellerNumber;
        SellerName = result.Value.Registration.Name;
        MaxArticleCount = result.Value.Seller.MaxArticleCount;
        Items = result.Value.Articles;
        AvailableCount = Items.Length;
        AvailableTotalValue = Items.Sum(a => a.Article.Price);
        Commission =  @event.Value.Commission;
        var sold = Items.Where(i => i.Checkout?.IsCompleted == true);
        SoldCount = sold.Count();
        SoldTotalValue = sold.Sum(a => a.Article.Price);
        PayoutTotalValue = converter.CalcPayout(@event.Value, SoldTotalValue);
    }
}
