using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace GtKram.Ui.Pages.MyBillings;

[Node("Kassenvorgänge", FromPage = typeof(IndexModel))]
[Authorize(Roles = "billing")]
public class BazaarBillingModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; private set; } = "Unbekannt";
    public BazaarBillingWithTotals[] Items { get; private set; } = [];

    public BazaarBillingModel(
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
        var result = await _mediator.Send(new CreateBillingByUserCommand(User.GetId(), eventId), cancellationToken);
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
        var result = await _mediator.Send(new GetBillingsWithTotalsAndEventByUserQuery(User.GetId(), eventId), cancellationToken);
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

        Items = result.Value.Billings;
    }
}
