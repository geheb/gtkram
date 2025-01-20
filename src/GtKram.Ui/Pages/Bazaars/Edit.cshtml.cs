using GtKram.Application.Converter;
using GtKram.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Bearbeiten", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public class EditModel : PageModel
{
    private readonly IBazaarEvents _bazaarEvents;
    private readonly ISellerRegistrations _sellerRegistrations;

    public Guid? Id { get; set; }

    public bool IsDisabled { get; set; }
    public string? EventDetails { get; set; }
    public bool HasRegistrations { get; set; }

    [BindProperty]
    public BazaarEventInput Input { get; set; } = new();

    public EditModel(IBazaarEvents bazaarEvents, ISellerRegistrations sellerRegistrations)
    {
        _bazaarEvents = bazaarEvents;
        _sellerRegistrations = sellerRegistrations;
    }

    public async Task OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Id = id;
        var dto = await _bazaarEvents.Find(id, cancellationToken);
        if (dto == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden");
            return;
        }

        EventDetails = dto.FormatEvent(new GermanDateTimeConverter());
        HasRegistrations = dto.SellerRegistrationCount > 0;

        Input = new BazaarEventInput();
        Input.From(dto);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        Id = id;
        var dto = await _bazaarEvents.Find(id, cancellationToken);
        if (dto == null)
        {
            IsDisabled = true;
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden!");
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        var error = Input.Validate();
        if (!string.IsNullOrEmpty(error))
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        Input.To(dto);
        var result = await _bazaarEvents.Update(dto, cancellationToken);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Speichern des Kinderbasars.");
            return Page();
        }

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bazaarEvents.Delete(id, cancellationToken);
        return new JsonResult(result);
    }
}
