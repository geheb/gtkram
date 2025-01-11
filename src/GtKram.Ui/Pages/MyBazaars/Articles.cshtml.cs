using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Artikel", FromPage = typeof(IndexModel))]
[Authorize(Roles = "seller,admin")]
public class ArticlesModel : PageModel
{
    private readonly BazaarSellerArticles _bazaarSellerArticles;
    private readonly BazaarSellers _bazaarSellers;

    public Guid? BazaarId { get; set; }
    public string? Event { get; set; }
    public string? EditArticleEndDate { get; set; }
    public int MaxArticleCount { get; set; }
    public bool CanEdit { get; set; }
    public bool CanAdd { get; set; }
    public bool IsRegisterAccepted { get; set; }
    public int AvailableCount { get; set; }
    public decimal AvailableTotalValue { get; set; }
    public int SoldCount { get; set; }
    public decimal SoldTotalValue { get; set; }
    public BazaarSellerArticleDto[] Articles { get; set; } = Array.Empty<BazaarSellerArticleDto>();
    public decimal PaymentTotalValue { get; set; }

    public ArticlesModel(BazaarSellerArticles bazaarSellerArticles, BazaarSellers bazaarSellers)
    {
        _bazaarSellerArticles = bazaarSellerArticles;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid bazaarId, CancellationToken cancellationToken)
    {
        BazaarId = bazaarId;
        var currentUserId = User.GetId();

        var seller = await _bazaarSellers.Find(bazaarId, cancellationToken);
        if (seller == null || seller.UserId != currentUserId)
        {
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden.");
            return;
        }

        var dc = new GermanDateTimeConverter();

        Event = seller.FormatEvent(dc);
        MaxArticleCount = seller.MaxArticleCount;
        EditArticleEndDate = dc.ToDateTime(seller.EditArticleEndDate);
        CanEdit = seller.IsRegisterAccepted && !seller.EditArticleExpired;
        CanAdd = seller.IsRegisterAccepted && !seller.EditArticleExpired && seller.CanAddArticle;
        

        if (!seller.IsRegisterAccepted)
        {
            ModelState.AddModelError(string.Empty, "Die Teilnahme wurde nicht zugesagt.");
        }
        else if (seller.EditArticleExpired)
        {
            ModelState.AddModelError(string.Empty, "Die Bearbeitung ist abgelaufen.");
        }

        Articles = await _bazaarSellerArticles.GetAll(bazaarId, currentUserId, cancellationToken);

        AvailableCount = Articles.Length;
        AvailableTotalValue = Articles.Sum(a => a.Price);

        var commission = seller.Commission;
        var sold = Articles.Where(a => a.IsSold);
        SoldCount = sold.Count();
        SoldTotalValue = sold.Sum(a => a.Price);
        PaymentTotalValue = SoldTotalValue - SoldTotalValue * (commission / 100.0M);
    }

    public async Task<IActionResult> OnPostTakeOverArticlesAsync(Guid bazaarId, CancellationToken cancellationToken)
    {
        var result = await _bazaarSellerArticles.TakeOverArticles(bazaarId, User.GetId(), cancellationToken);
        return new JsonResult(result);
    }
}
