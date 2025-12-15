using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public class EditModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public bool IsDisabled { get; set; }

    [BindProperty]
    public BazaarEventInput Input { get; set; } = new();

    public EditModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindEventQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(result.Value, _timeProvider))
        {
            ModelState.AddError(Domain.Errors.Event.Expired);
        }

        Input.Init(result.Value);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToUpdateCommand(id), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
