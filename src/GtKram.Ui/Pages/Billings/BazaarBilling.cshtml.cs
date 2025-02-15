using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Billings;

[Node("Kassen-Vorgänge", FromPage = typeof(IndexModel))]
[Authorize(Roles = "billing,admin")]
public class BazaarBillingModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; set; } = "Unbekannt";

    public BazaarBillingWithTotals[] Items { get; private set; } = [];

    public BazaarBillingModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBazaarBillingsWithTotalsAndEventQuery(eventId), cancellationToken);
        if(result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Event = eventConverter.Format(result.Value.Event);

        if (eventConverter.IsExpired(result.Value.Event, _timeProvider))
        {
            ModelState.AddModelError(string.Empty, Domain.Errors.Event.Expired.Message);
            return;
        }

        Items = result.Value.Billings;
    }
}
