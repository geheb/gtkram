using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Authorize(Roles = "manager,admin")]
public class PrintSellerArticlesModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IBazaarSellers _bazaarSellers;
    private readonly IBazaarSellerArticles _bazaarSellerArticles;

    public string? Event { get; set; }
    public string? SellerName { get; set; }
    public int SellerNumber { get; set; }
    public int AvailableCount { get; set; }
    public BazaarSellerArticleDto[] Articles { get; private set; } = [];

    public PrintSellerArticlesModel(
        ILogger<PrintSellerArticlesModel> logger,
        IBazaarSellers bazaarSellers, 
        IBazaarSellerArticles bazaarSellerArticles)
    {
        _logger = logger;
        _bazaarSellers = bazaarSellers;
        _bazaarSellerArticles = bazaarSellerArticles;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
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
        SellerName = dto.RegistrationName;

        if (dto.UserId.HasValue)
        {
            Articles = await _bazaarSellerArticles.GetAll(dto.Id!.Value, dto.UserId.Value, cancellationToken);
        }

        AvailableCount = Articles.Length;
    }
}
