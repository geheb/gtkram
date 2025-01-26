using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Models;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Billings;

[Node("Artikel anlegen", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "billing,admin")]
public class ArticleAddModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IBazaarEvents _bazaarEvents;
    private readonly IBazaarBillings _bazaarBillings;
    private readonly IBazaarBillingArticles _bazaarBillingArticles;
    private readonly IBazaarSellers _bazaarSellers;

    public Guid? EventId { get; set; }
    public Guid? BillingId { get; set; }
    public string? EventNameAndDescription { get; private set; }

    [BindProperty, Display(Name = "Verkäufernummer")]
    [RequiredField]
    [Range(1, 999, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int? SellerNumber { get; set; }

    [BindProperty, Display(Name = "Artikelnummer")]
    [RequiredField]
    [Range(1, 999, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int? LabelNumber { get; set; }

    public ArticleAddModel(
        ILogger<ArticleAddModel> logger,
        IBazaarEvents bazaarEvents,
        IBazaarBillings bazaarBillings,
        IBazaarBillingArticles bazaarBillingArticles,
        IBazaarSellers bazaarSellers)
    {
        _logger = logger;
        _bazaarEvents = bazaarEvents;
        _bazaarBillings = bazaarBillings;
        _bazaarBillingArticles = bazaarBillingArticles;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        EventId = eventId;
        BillingId = billingId;

        if (!await Validate(eventId, billingId, cancellationToken)) return;
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        EventId = eventId;
        BillingId = billingId;

        if (!await Validate(eventId, billingId, cancellationToken)) return Page();

        var result = await _bazaarBillingArticles.Create(eventId, billingId, SellerNumber!.Value, LabelNumber!.Value, cancellationToken);

        switch (result.status)
        {
            case BazaarArticleStatus.Exists:
            case BazaarArticleStatus.Created:
                return RedirectToPage("Articles",
                    new 
                    { 
                        eventId, 
                        billingId, 
                        result.billingArticleId, 
                        message = result.status == BazaarArticleStatus.Exists ? 1 : 0
                    });

            case BazaarArticleStatus.ArticelNotFound:
                ModelState.AddModelError(string.Empty, LocalizedMessages.ArticleNotFound);
                break;
            case BazaarArticleStatus.SellerNotFound:
                ModelState.AddModelError(string.Empty, LocalizedMessages.SellerNotFound);
                break;
            case BazaarArticleStatus.SaveFailed:
                ModelState.AddModelError(string.Empty, LocalizedMessages.SaveFailed);
                break;
        }

        return Page();
    }

    private async Task<bool> Validate(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return false;

        if (eventId == Guid.Empty || billingId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return false;
        }

        var @event = await _bazaarEvents.Find(eventId, cancellationToken);
        if (@event == null)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarNotFound);
            return false;
        }

        EventNameAndDescription = @event.FormatEvent(new GermanDateTimeConverter());

        if (@event.IsBillingExpired)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarExpired);
        }

        var billing = await _bazaarBillings.Find(eventId, billingId, cancellationToken);
        if (billing == null)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BillingNotFound);
            return false;
        }

        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Manager))
        {
            var seller = await _bazaarSellers.Find(eventId, User.GetId(), cancellationToken);
            if (seller == null)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.SellerNotFound);
                return false;
            }
            if (!seller.CanCreateBillings)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.CreateBillingsForbidden);
                return false;
            }
        }

        return true;
    }
}
