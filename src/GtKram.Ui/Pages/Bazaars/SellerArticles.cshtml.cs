using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Artikel", FromPage = typeof(EditSellerModel))]
[Authorize(Roles = "manager,admin")]
public class SellerArticlesModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IBazaarSellers _bazaarSellers;
    private readonly IBazaarSellerArticles _bazaarSellerArticles;

    public Guid? EventId { get; set; }
    public Guid? Id { get; set; }

    public string? Event { get; set; }
    public string? SellerName { get; set; }
    public int SellerNumber { get; set; }
    public int MaxArticleCount { get; set; }
    public int AvailableCount { get; set; }
    public decimal AvailableTotalValue { get; set; }
    public int SoldCount { get; set; }
    public decimal SoldTotalValue { get; set; }
    public int Commission { get; set; }
    public decimal PaymentTotalValue { get; set; }
    public BazaarSellerArticleDto[] Articles { get; private set; } = [];

    public SellerArticlesModel(
        ILogger<SellerArticlesModel> logger,
        IBazaarSellers bazaarSellers, 
        IBazaarSellerArticles bazaarSellerArticles)
    {
        _logger = logger;
        _bazaarSellers = bazaarSellers;
        _bazaarSellerArticles = bazaarSellerArticles;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        EventId = eventId;
        Id = id;

        if (eventId == Guid.Empty || id == Guid.Empty)
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        var dto = await _bazaarSellers.Find(id, cancellationToken);
        if (dto == null)
        {
            ModelState.AddModelError(string.Empty, "Verkäufer wurde nicht gefunden.");
            return;
        }

        Event = dto.FormatEvent(new GermanDateTimeConverter());
        SellerNumber = dto.SellerNumber;
        MaxArticleCount = dto.MaxArticleCount;
        SellerName = dto.RegistrationName;

        if (dto.UserId.HasValue)
        {
            Articles = await _bazaarSellerArticles.GetAll(dto.Id!.Value, dto.UserId.Value, cancellationToken);
        }

        AvailableCount = Articles.Length;
        AvailableTotalValue = Articles.Sum(a => a.Price);

        Commission = dto.Commission;
        var sold = Articles.Where(a => a.IsSold);
        SoldCount = sold.Count();
        SoldTotalValue = sold.Sum(a => a.Price);
        PaymentTotalValue = SoldTotalValue - SoldTotalValue * (Commission / 100.0M);
    }
}
