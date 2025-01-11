using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Billings;

[Node("Kassen-Vorgänge", FromPage = typeof(IndexModel))]
[Authorize(Roles = "billing,admin")]
public class BazaarBillingModel : PageModel
{
    private readonly ILogger _logger;
    private readonly BazaarEvents _bazaarEvents;
    private readonly BazaarBillings _bazaarBillings;
    private readonly BazaarSellers _bazaarSellers;

    public Guid? EventId { get; set; }
    public string? EventNameAndDescription { get; private set; }
    public BazaarBillingDto[] Items { get; private set; } = [];
    public bool CanCreateBilling { get; private set; }

    public BazaarBillingModel(
        ILogger<BazaarBillingModel> logger,
        BazaarEvents bazaarEvents, 
        BazaarBillings bazaarBillings,
        BazaarSellers bazaarSellers)
    {
        _logger = logger;
        _bazaarEvents = bazaarEvents;
        _bazaarBillings = bazaarBillings;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        EventId = eventId;
        if (eventId == Guid.Empty)
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }
        var @event = await _bazaarEvents.Find(eventId, cancellationToken);
        if (@event == null)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarNotFound);
            return;
        }

        EventNameAndDescription = @event.FormatEvent(new());

        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Manager))
        {
            var seller = await _bazaarSellers.Find(eventId, User.GetId(), cancellationToken);
            if (seller == null)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.SellerNotFound);
                return;
            }
            if (!seller.CanCreateBillings)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.CreateBillingsForbidden);
            }
        }

        if (@event.IsBillingExpired)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarExpired);
        }

        if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager))
        {
            Items = await _bazaarBillings.GetAll(eventId, cancellationToken);
        }
        else
        {
            Items = await _bazaarBillings.GetAll(User.GetId(), eventId, cancellationToken);
        }
    }

    public async Task<IActionResult> OnGetCreateAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }
        var @event = await _bazaarEvents.Find(eventId, cancellationToken);
        if (@event == null)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarNotFound);
            return Page();
        }

        if (@event.IsBillingExpired)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarExpired);
            return Page();
        }

        var billingId = await _bazaarBillings.Create(eventId, User.GetId(), cancellationToken);
        return RedirectToPage("Articles", new { eventId, billingId });
    }
}
