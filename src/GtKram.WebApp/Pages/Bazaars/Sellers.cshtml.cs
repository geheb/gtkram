using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Registrierungen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class SellersModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    public string Event { get; set; } = "Unbekannt";
    public SellerRegistrationWithArticleCount[] Items { get; set; } = [];

    public int AcceptedCount { get; set; }
    public int CancelledCount { get; set; }
    public int UnconfirmedCount { get; set; }
    public int AcceptedWithoutArticleCount { get; set; }
    public int ArticleCount { get; set; }
    public bool IsExpired { get; set; }

    public SellersModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await _mediator.Send(new FindEventQuery(eventId), cancellationToken);
        if (@event.IsFailed)
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
        Items = await _mediator.Send(new GetSellerRegistrationWithArticleCountQuery(eventId), cancellationToken);

        AcceptedCount = Items.Count(r => r.Registration.IsAccepted == true);
        CancelledCount = Items.Count(r => r.Registration.IsAccepted == false);
        UnconfirmedCount = Items.Count(r => r.Registration.IsAccepted is null);
        AcceptedWithoutArticleCount = Items.Count(r => r.Registration.IsAccepted == true && r.ArticleCount == 0);
        ArticleCount = Items.Sum(r => r.ArticleCount);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSellerRegistrationCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid id, CancellationToken cancellationToken)
    {
        var callbackUrl = Url.PageLink("/Login/ConfirmRegistration", values: new { id = Guid.Empty, token = string.Empty });
        var result = await _mediator.Send(new AcceptSellerRegistrationCommand(id,  callbackUrl!), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }

    public async Task<IActionResult> OnPostDenyAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DenySellerRegistrationCommand(id), cancellationToken);
        return new JsonResult(result.IsSuccess);
    }
}
