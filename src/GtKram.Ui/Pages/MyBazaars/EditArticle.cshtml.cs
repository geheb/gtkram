using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Core.User;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyBazaars;

[Node("Artikel bearbeiten", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "seller,admin")]
public class EditArticleModel : PageModel
{
    private readonly BazaarSellerArticles _bazaarSellerArticles;
    private readonly BazaarSellers _bazaarSellers;

    public Guid? BazaarId { get; set; }
    public Guid? Id { get; set; }
    public string? Event { get; set; }
    public bool IsDisabled { get; set; }

    [BindProperty]
    public ArticleInput Input { get; set; } = new();

    public EditArticleModel(BazaarSellerArticles bazaarSellerArticles, BazaarSellers bazaarSellers)
    {
        _bazaarSellerArticles = bazaarSellerArticles;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid bazaarId, Guid id, CancellationToken cancellationToken)
    {
        BazaarId = bazaarId;
        Id = id;

        var seller = await _bazaarSellers.Find(bazaarId, cancellationToken);
        if (seller == null || seller.UserId != User.GetId())
        {
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden.");
            IsDisabled = true;
            return;
        }

        Event = seller.FormatEvent(new());

        if (!seller.IsRegisterAccepted)
        {
            ModelState.AddModelError(string.Empty, "Die Teilnahme wurde nicht zugesagt.");
            IsDisabled = true;
            return;
        }

        if (seller.EditArticleExpired)
        {
            ModelState.AddModelError(string.Empty, "Die Bearbeitung ist abgelaufen.");
            IsDisabled = true;
            return;
        }

        var article = await _bazaarSellerArticles.Find(User.GetId(), id, cancellationToken);
        if (article == null)
        {
            ModelState.AddModelError(string.Empty, "Artikel wurde nicht gefunden.");
            IsDisabled = true;
            return;
        }

        if (!article.CanEdit)
        {
            ModelState.AddModelError(string.Empty, "Artikel ist bereits gebucht.");
            IsDisabled = true;
        }

        Input = new ArticleInput();
        Input.From(article);
    }

    public async Task<IActionResult> OnPostAsync(Guid bazaarId, Guid id, CancellationToken cancellationToken)
    {
        BazaarId = bazaarId;
        Id = id;

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

        if (!ModelState.IsValid) return Page();

        if (!Input.HasPriceClosestToFifty)
        {
            ModelState.AddModelError(string.Empty, "Der Preis sollte in 50 Cent Schritten angegeben werden.");
            return Page();
        }

        var article = new BazaarSellerArticleDto();
        Input.Id = id;
        Input.To(article);

        var result = await _bazaarSellerArticles.Update(bazaarId, User.GetId(), article, cancellationToken);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.SaveFailed);
            return Page();
        }

        return RedirectToPage("Articles", new { bazaarId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bazaarSellerArticles.Delete(User.GetId(), id, cancellationToken);
        return new JsonResult(result);
    }
}
