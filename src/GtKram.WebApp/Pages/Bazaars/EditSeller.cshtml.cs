using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Errors;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Verkäufer bearbeiten", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public sealed class EditSellerModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    [BindProperty]
    public SellerRegistrationInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public EditSellerModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (@event.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(@event.Errors);
            return;
        }

        var sellerResult = await _mediator.Send(new FindRegistrationWithSellerQuery(id), cancellationToken);
        if (sellerResult.IsError)
        {
            IsDisabled = true;
            ModelState.AddError(sellerResult.Errors);
            return;
        }

        var converter = new EventConverter();
        Input.State_Event = converter.Format(@event.Value);

        var isExpired = converter.IsExpired(@event.Value, _timeProvider);
        if (isExpired)
        {
            ModelState.AddError(Event.Expired);
        }

        IsDisabled = sellerResult.Value.Seller is null || isExpired;
        Input.InitDefault(sellerResult.Value.Registration);
        if (sellerResult.Value.Seller is not null)
        {
            Input.Init(sellerResult.Value.Seller);
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToCommand(id), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Sellers", new { eventId });
    }
}
