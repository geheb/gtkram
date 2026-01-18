using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GtKram.WebApp.Pages.Plannings;

[Node("Zeitplan anlegen", FromPage = typeof(SchedulesModel))]
[Authorize(Roles = "manager,admin")]
public sealed class AddScheduleModel : PageModel
{
    private TimeProvider _timeProvider;
    private IMediator _mediator;

    [BindProperty]
    public ScheduleInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public SelectListItem[] Users { get; set; } = [];

    public Dictionary<Guid, string> UserMap { get; set; } = [];

    public AddScheduleModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (result.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var converter = new EventConverter();
        Input.State_Event = converter.Format(result.Value);

        if (converter.IsExpired(result.Value, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Event.Expired);
        }

        await FillUsers(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await FillUsers(cancellationToken);

        if (!ModelState.IsValid) return Page();

        var command = Input.ToCreateCommand(eventId);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Schedules", new { eventId });
    }

    private async Task FillUsers(CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(new GetAllUsersQuery(UserRoleType.Helper), cancellationToken);
        UserMap = users.ToDictionary(u => u.Id, u => $"{u.Name} ({u.Email})");
        
        Users =
        [
            new(),
            .. users.Select(u => new SelectListItem(UserMap[u.Id], u.Id.ToString())),
        ];
    }
}