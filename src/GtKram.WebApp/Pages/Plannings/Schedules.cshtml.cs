using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Plannings;

[Node("Zeitpläne", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public sealed class SchedulesModel : PageModel
{
    private TimeProvider _timeProvider;
    private IMediator _mediator;

    public string Event { get; set; } = "Unbekannt";
    public Planning[] Items { get; set; } = [];
    public bool IsExpired { get; set; }

    public SchedulesModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (@event.IsError)
        {
            ModelState.AddError(@event.Errors);
            return;
        }

        var converter = new EventConverter();
        IsExpired = converter.IsExpired(@event.Value, _timeProvider);
        if (IsExpired)
        {
            ModelState.AddError(Domain.Errors.Event.Expired);
        }

        Event = converter.Format(@event.Value);

        Items = await _mediator.Send(new GetPlanningsQuery(eventId), cancellationToken);
    }
}