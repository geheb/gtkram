using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Checkouts;

[Node("Kassenvorgänge", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public sealed class CheckoutModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; set; } = "Unbekannt";

    public CheckoutWithTotals[] Items { get; private set; } = [];

    public CheckoutModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCheckoutWithTotalsAndEventQuery(eventId), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Event = eventConverter.Format(result.Value.Event);

        if (eventConverter.IsExpired(result.Value.Event, _timeProvider))
        {
            ModelState.AddError(Domain.Errors.Event.Expired);
        }

        Items = result.Value.Checkouts;
    }
}
