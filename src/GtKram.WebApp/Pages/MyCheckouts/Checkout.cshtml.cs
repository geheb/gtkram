using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.MyCheckouts;

[Node("Kassenvorgänge", FromPage = typeof(IndexModel))]
[Authorize(Roles = "checkout")]
public class CheckoutModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; private set; } = "Unbekannt";
    public CheckoutWithTotals[] Items { get; private set; } = [];

    public CheckoutModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken) =>
        await UpdateView(eventId, cancellationToken);

    public async Task<IActionResult> OnGetCreateAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCheckoutByUserCommand(User.GetId(), eventId), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            await UpdateView(eventId, cancellationToken);
            return Page();
        }

        return RedirectToPage("Articles", new { eventId, id = result.Value });
    }

    private async Task UpdateView(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCheckoutWithTotalsAndEventByUserQuery(User.GetId(), eventId), cancellationToken);
        if (result.IsFailed)
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
