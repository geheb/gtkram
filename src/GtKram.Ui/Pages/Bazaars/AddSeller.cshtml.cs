using GtKram.Core.Email;
using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verkäufer anlegen", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public class AddSellerModel : PageModel
{
    private readonly BazaarEvents _bazaarEvents;
    private readonly SellerRegistrations _sellerRegistrations;
    private readonly EmailValidatorService _emailValidator;

    [BindProperty]
    public AddSellerInput Input { get; set; } = new();

    public Guid? EventId { get; set; }
    public bool IsDisabled { get; set; }
    public string? Details { get; set; }

    public AddSellerModel(
        BazaarEvents bazaarEvents,
        SellerRegistrations sellerRegistrations,
        EmailValidatorService emailValidator)
    {
        _bazaarEvents = bazaarEvents;
        _sellerRegistrations = sellerRegistrations;
        _emailValidator = emailValidator;
    }
    public async Task OnGetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await UpdateView(eventId, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (!await UpdateView(eventId, cancellationToken)) return Page();

        if (!await _emailValidator.Validate(Input.Email!, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Die E-Mail-Addresse ist ungültig.");
            return Page();
        }

        var created = await _sellerRegistrations.Register(eventId, Input.Email!, Input.Name!, Input.Phone!, cancellationToken);
        if (!created)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Anlegen des Verkäufers.");
            return Page();
        }

        return RedirectToPage("Sellers", new { EventId = eventId });
    }

    private async Task<bool> UpdateView(Guid id, CancellationToken cancellationToken)
    {
        EventId = id;
        var dto = await _bazaarEvents.Find(id, cancellationToken);
        if (dto == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden.");
            return false;
        }

        Details = dto.FormatEvent(new GermanDateTimeConverter());

        return ModelState.IsValid;
    }
}
