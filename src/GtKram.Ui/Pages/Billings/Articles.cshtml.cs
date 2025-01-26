using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Models;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace GtKram.Ui.Pages.Billings;

[Node("Kassen-Vorgang", FromPage = typeof(BazaarBillingModel))]
[Authorize(Roles = "billing,admin")]
public class ArticlesModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IBazaarEvents _bazaarEvents;
    private readonly IBazaarBillings _bazaarBillings;
    private readonly IBazaarBillingArticles _bazaarBillingArticles;
    private readonly IBazaarSellers _bazaarSellers;

    public Guid? EventId { get; private set; }
    public Guid? BillingId { get; private set; }
    public string? EventNameAndDescription { get; private set; }
    public IEnumerable<BazaarBillingArticleDto> Articles { get; private set; } = [];
    public BazaarBillingArticleDto? AddedArticle { get; private set; }
    public bool AddedArticleExists { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanComplete { get; private set; }

    public ArticlesModel(
        ILogger<ArticlesModel> logger,
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

    public async Task OnGetAsync(Guid eventId, Guid billingId, Guid? billingArticleId, int? message, CancellationToken cancellationToken)
    {
        EventId = eventId;
        BillingId = billingId;

        if (eventId == Guid.Empty || billingId == Guid.Empty)
        {
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

        var billing = await _bazaarBillings.Find(eventId, billingId, cancellationToken);
        if (billing == null)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BillingNotFound);
            return;
        }

        var isAdminOrManager = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);

        if (!isAdminOrManager)
        {
            if (billing.UserId != User.GetId())
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.CreateBillingsForbidden);
                return;
            }
            var seller = await _bazaarSellers.Find(eventId, User.GetId(), cancellationToken);
            if (seller == null)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.SellerNotFound);
                return;
            }
            if (!seller.CanCreateBillings)
            {
                ModelState.AddModelError(string.Empty, LocalizedMessages.CreateBillingsForbidden);
                return;
            }
        }

        if (@event.IsBillingExpired)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarExpired);
        }

        CanEdit =
            isAdminOrManager ||
            billing.Status == BillingStatus.InProgress && billing.UserId == User.GetId();

        if (billingArticleId.HasValue && message.HasValue)
        {
            AddedArticle = await _bazaarBillingArticles.Find(billingArticleId.Value, cancellationToken);
            AddedArticleExists = message.Value == 1;
        }

        Articles = await _bazaarBillingArticles.GetAll(eventId, billingId, cancellationToken);

        CanComplete = Articles.Any() && CanEdit;
    }

    public async Task<IActionResult> OnPostAddAsync(Guid eventId, Guid billingId, Guid articleId, CancellationToken cancellationToken)
    {
        var result = await _bazaarBillingArticles.Create(eventId, billingId, articleId, cancellationToken);

        if (!result.billingArticleId.HasValue) return new JsonResult(null);

        var article = await _bazaarBillingArticles.Find(result.billingArticleId.Value, cancellationToken);

        return new JsonResult(new
        {
            addedOn = new GermanDateTimeConverter().ToDateTime(article!.AddedOn),
            sellerNumber = article.SellerNumber,
            lableNumber = article.LabelNumber,
            name = article.Name,
            price = article.Price,
            exists = result.status == BazaarArticleStatus.Exists
        });
    }

    public async Task<IActionResult> OnPostSumAsync(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var result = await _bazaarBillings.Find(eventId, billingId, cancellationToken);
        if (result == null)
        {
            return new JsonResult(null);
        }
        return new JsonResult(new { count = result.ArticleCount, total = result.Total.ToString("0.00", CultureInfo.InvariantCulture) });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid eventId, Guid billingId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _bazaarBillingArticles.Delete(eventId, billingId, id, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var result = await _bazaarBillingArticles.Cancel(eventId, billingId, cancellationToken);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid eventId, Guid billingId, CancellationToken cancellationToken)
    {
        var result = await _bazaarBillings.SetAsCompleted(eventId, billingId, cancellationToken);
        return new JsonResult(result);
    }
}
