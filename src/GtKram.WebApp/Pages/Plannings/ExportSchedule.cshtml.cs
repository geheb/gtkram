using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Models;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Plannings;

[Authorize(Roles = "manager,admin")]
public sealed class ExportScheduleModel : PageModel
{
    private IMediator _mediator;

    public string? Event { get; set; } = "Unbekannt";
    public Planning[] Items { get; set; } = [];
    public Dictionary<Guid, string> UserMap { get; set; } = [];

    public ExportScheduleModel(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        Items = await _mediator.Send(new GetPlanningsQuery(eventId), cancellationToken);

        var eventOrError = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (eventOrError.IsError)
        {
            return;
        }

        var users = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
        UserMap = users.ToDictionary(u => u.Id, u => u.Name);

        Event = new EventConverter().Format(eventOrError.Value);
    }
}
