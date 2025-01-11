using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Verkäufer bearbeiten", FromPage = typeof(SellersModel))]
[Authorize(Roles = "manager,admin")]
public class EditSellerModel : PageModel
{
    private readonly ILogger _logger;
    private readonly BazaarSellers _bazaarSellers;

    public Guid? EventId { get; set; }
    public Guid? Id { get; set; }

    public string? Event { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    public string? Email { get; set; }

    [Display(Name = "Telefonnummer")]
    public string? Phone { get; set; }

    // see also BazaarEventInput.MaxSellers
    [Display(Name = "Verkäufernummer")]
    [BindProperty, RequiredField, Range(0, 200, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int SellerNumber { get; set; }

    [Display(Name = "Rolle des Verkäufers")]
    [BindProperty, RequiredField, Range((int)SellerRole.Standard, (int)SellerRole.Orga, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int Role { get; set; }

    [Display(Name = "Darf kassieren")]
    [BindProperty, RequiredField]
    public bool CanCreateBillings { get; set; }

    public bool IsDisabled { get; set; }

    public EditSellerModel(
        ILogger<EditSellerModel> logger,
        BazaarSellers bazaarSellers)
    {
        _logger = logger;
        _bazaarSellers = bazaarSellers;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        EventId = eventId;
        Id = id;

        if (eventId == Guid.Empty || id == Guid.Empty)
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        var dto = await _bazaarSellers.Find(id, cancellationToken);
        if (dto == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Verkäufer wurde nicht gefunden.");
            return;
        }

        if (dto.IsEventExpired)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.BazaarExpired);
        }

        Event = dto.FormatEvent(new GermanDateTimeConverter());
        Name = dto.RegistrationName;
        Email = dto.RegistrationEmail;
        Phone = dto.RegistrationPhone;
        SellerNumber = dto.SellerNumber;
        Role = (int)dto.Role;
        CanCreateBillings = dto.CanCreateBillings;
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        EventId = eventId;
        Id = id;

        if (eventId == Guid.Empty || id == Guid.Empty)
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        var dto = await _bazaarSellers.Find(id, cancellationToken);
        if (dto == null)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, "Verkäufer wurde nicht gefunden.");
            return Page();
        }

        Event = dto.FormatEvent(new GermanDateTimeConverter());
        Name = dto.RegistrationName;
        Email = dto.RegistrationEmail;
        Phone = dto.RegistrationPhone;

        if (!ModelState.IsValid) return Page();

        var result = await _bazaarSellers.Update(id, (SellerRole)Role, SellerNumber, CanCreateBillings, cancellationToken);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.SaveFailed);
            return Page();
        }

        return RedirectToPage("Sellers", new { eventId });
    }
}
