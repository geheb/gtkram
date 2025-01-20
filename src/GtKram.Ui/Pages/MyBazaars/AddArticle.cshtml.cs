using GtKram.Application.Repositories;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Anlegen", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "seller,admin")]
public class AddArticleModel : PageModel
{
    private readonly IBazaarSellerArticles _bazaarSellerArticles;
    private readonly IBazaarSellers _bazaarSellers;

    public Guid? BazaarId { get; set; }
    public bool IsDisabled { get; set; }
    public string? Event { get; set; }

    [BindProperty]
    public ArticleInput Input { get; set; } = new();

    public AddArticleModel(
        IBazaarSellerArticles bazaarSellerArticles, 
        IBazaarSellers bazaarSellers)
    {
        _bazaarSellerArticles = bazaarSellerArticles;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid bazaarId, CancellationToken cancellationToken)
    {
        BazaarId = bazaarId;

        var seller = await _bazaarSellers.Find(bazaarId, cancellationToken);
        if (seller == null)
        {
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden.");
            IsDisabled = true;
            return;
        }

        Event = seller.FormatEvent(new());

        if (seller.EditArticleExpired)
        {
            ModelState.AddModelError(string.Empty, "Die Bearbeitung ist bereits abgeschlossen.");
            IsDisabled = true;
            return;
        }

        if (!seller.IsRegisterAccepted)
        {
            ModelState.AddModelError(string.Empty, "Die Teilnahme wurde nicht zugesagt.");
            IsDisabled = true;
            return;
        }

        if (!seller.CanAddArticle)
        {
            ModelState.AddModelError(string.Empty, $"Die Anzahl der Artikel ist ausgeschöpft. Es können nur {seller.MaxArticleCount} Artikel verwaltet werden.");
            IsDisabled = true;
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid bazaarId, CancellationToken cancellationToken)
    {
        BazaarId = bazaarId;

        var seller = await _bazaarSellers.Find(bazaarId, cancellationToken);
        if (seller == null)
        {
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden.");
            IsDisabled = true;
            return Page();
        }

        Event = seller.FormatEvent(new());

        if (seller.EditArticleExpired)
        {
            ModelState.AddModelError(string.Empty, "Die Bearbeitung ist abgelaufen.");
            IsDisabled = true;
            return Page();
        }

        if (!seller.IsRegisterAccepted)
        {
            ModelState.AddModelError(string.Empty, "Die Teilnahme wurde nicht zugesagt.");
            IsDisabled = true;
            return Page();
        }

        if (!seller.CanAddArticle)
        {
            ModelState.AddModelError(string.Empty, $"Die Anzahl der Artikel ist ausgeschöpft. Es können nur {seller.MaxArticleCount} Artikel verwaltet werden.");
            IsDisabled = true;
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        if (!Input.HasPriceClosestToFifty)
        {
            ModelState.AddModelError(string.Empty, "Der Preis sollte in 50 Cent Schritten angegeben werden.");
            return Page();
        }

        var article = new BazaarSellerArticleDto();
        Input.To(article);

        var result = await _bazaarSellerArticles.Create(bazaarId, User.GetId(), article, cancellationToken);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.SaveFailed);
            return Page();
        }

        return RedirectToPage("Articles", new { bazaarId });
    }
}
