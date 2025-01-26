using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Registrierungen", FromPage = typeof(EditModel))]
[Authorize(Roles = "manager,admin", Policy = Policies.TwoFactorAuth)]
public class SellersModel : PageModel
{
    private readonly IBazaarEvents _bazaarEvents;
    private readonly ISellerRegistrations _sellerRegistrations;

    public Guid? EventId { get; set; }
    public string? Event { get; set; }
    public BazaarSellerRegistrationDto[] Registrations { get; set; } = [];

    [BindProperty]
    public string[] SelectedRegistrations { get; set; } = [];

    public int Count { get; set; }
    public int AcceptedCount { get; set; }
    public int CancelledCount { get; set; }
    public int UnconfirmedCount { get; set; }
    public int AcceptedWithoutArticleCount { get; set; }
    public int ArticleCount { get; set; }
    public bool IsExpired { get; set; }

    public SellersModel(
        IBazaarEvents bazaarEvents,
        ISellerRegistrations sellerRegistrations)
    {
        _bazaarEvents = bazaarEvents;
        _sellerRegistrations = sellerRegistrations;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        EventId = eventId;

        var @event = await _bazaarEvents.Find(eventId, cancellationToken);
        Registrations = Array.Empty<BazaarSellerRegistrationDto>();

        if (@event != null)
        {
            IsExpired = @event.IsBillingExpired;
            Event = @event.FormatEvent(new());
            Registrations = await _sellerRegistrations.GetAll(eventId, cancellationToken);
            Count = Registrations.Length;
            AcceptedCount = Registrations.Count(r => r.Accepted == true);
            CancelledCount = Registrations.Count(r => r.Accepted == false);
            UnconfirmedCount = Registrations.Count(r => !r.Accepted.HasValue);
            AcceptedWithoutArticleCount = Registrations.Count(r => r.Accepted == true && r.ArticleCount.GetValueOrDefault() == 0);
            ArticleCount = Registrations.Sum(r => r.ArticleCount.GetValueOrDefault());
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid eventId, Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _sellerRegistrations.Delete(eventId, sellerId, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid eventId, Guid sellerId, CancellationToken cancellationToken)
    {
        var callbackUrl = Url.PageLink("/Login/ConfirmRegistration", values: new { id = Guid.Empty, token = string.Empty });
        var result = await _sellerRegistrations.Confirm(eventId, sellerId, true, callbackUrl!, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostDenyAsync(Guid eventId, Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await _sellerRegistrations.Confirm(eventId, sellerId, false, string.Empty, cancellationToken);
        return new JsonResult(result);
    }
}
