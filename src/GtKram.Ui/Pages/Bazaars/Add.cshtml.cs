using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Anlegen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public class AddModel : PageModel
{
    private readonly BazaarEvents _bazaarEvents;

    [BindProperty]
    public BazaarEventInput Input { get; set; } = new();

    public AddModel(BazaarEvents bazaarEvents, IOptions<AppSettings> appSettings)
    {
        _bazaarEvents = bazaarEvents;
        Input.Address = appSettings.Value.DefaultEventLocation;
    }

    public void OnGet()
    {
        Input = new BazaarEventInput();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var error = Input.Validate();
        if (!string.IsNullOrEmpty(error))
        {
            ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        var dto = new BazaarEventDto();
        Input.To(dto);

        var result = await _bazaarEvents.Create(dto, cancellationToken);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Anlegen des Kinderbasars.");
            return Page();
        }

        return RedirectToPage("Index");
    }
}
