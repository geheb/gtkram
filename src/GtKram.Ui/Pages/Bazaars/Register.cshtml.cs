using GtKram.Core.Email;
using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using GtKram.Ui.Annotations;
using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly ILogger _logger;
    private readonly BazaarEvents _bazaarEvents;
    private readonly SellerRegistrations _sellerRegistrations;
    private readonly EmailValidatorService _emailValidator;

    private enum State { Success, RequestFailed, EventNotFound, RegisterClosed }

    public string? BazaarNameAndDescription { get; set; }
    public string? BazaarAddress { get; set; }

    [BindProperty]
    public string? SellerUserName { get; set; } // just for bots

    [BindProperty, Display(Name = "Name")]
    [RequiredField, StringLength(64, MinimumLength = 2, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? SellerName { get; set; }

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? SellerEmail { get; set; }

    [BindProperty, Display(Name = "Telefonnummer")]
    [RequiredField, RegularExpression(@"^(\d{4,15})$", ErrorMessage = "Das Feld '{0}' muss zwischen 4 und 15 Zeichen liegen und darf nur Zahlen enthalten.")]
    public string? SellerPhone { get; set; }

    [BindProperty]
    public int[] SellerClothing { get; set; } = [];

    [BindProperty]
    public bool HasKita { get; set; }
    
    [BindProperty, Display(Name = "Die goldenen Regeln gelesen")]
    [RequireTrueField]
    public bool HasGoldenRules { get; set; }

    public bool IsDisabled { get; set; }
    public string? Message { get; set; }

    public RegisterModel(
        ILogger<RegisterModel> logger,
        BazaarEvents bazaarEvents,
        SellerRegistrations sellerRegistrations, 
        EmailValidatorService emailValidator)
    {
        _logger = logger;
        _bazaarEvents = bazaarEvents;
        _sellerRegistrations = sellerRegistrations;
        _emailValidator = emailValidator;
    }

    public async Task OnGetAsync(Guid id, bool? success, CancellationToken cancellationToken)
    {
        var state = await Update(id, cancellationToken);

        if (state != State.Success)
        {
            IsDisabled = true;
            switch (state)
            {
                case State.RequestFailed: ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest); break;
                case State.EventNotFound: ModelState.AddModelError(string.Empty, "Kinderbasar wurde nicht gefunden."); break;
                case State.RegisterClosed: ModelState.AddModelError(string.Empty, "Die Registrierung ist abgeschlossen. Es können keine weiteren Anfragen angenommen werden."); break;
            }
            return;
        }

        if (success.HasValue)
        {
            IsDisabled = true;
            Message = success.Value ?
                "Vielen Dank für die unverbindliche Registrierung. Du erhältst bald eine Zu- oder Absage per E-Mail." :
                "Es tut uns leid, aber die Registrierung kann nicht mehr angenommen werden.";
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        var state = await Update(id, cancellationToken);
        if (state != State.Success)
        {
            return RedirectToPage(string.Empty, state == State.RegisterClosed ? new { id, success = false } : new { id });
        }

        if (!ModelState.IsValid) return Page();

        if (!await _emailValidator.Validate(SellerEmail!, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Die E-Mail-Addresse ist ungültig."); 
            return Page();
        }

        var registerId = await _sellerRegistrations.Register(id, new BazaarSellerRegistrationDto
        {
            Name = SellerName,
            Email = SellerEmail,
            Phone = SellerPhone,
            Clothing = SellerClothing,
            HasKita = HasKita
        }, cancellationToken);

        return RedirectToPage(string.Empty, new { id, success = registerId.HasValue });
    }

    private async Task<State> Update(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || !string.IsNullOrEmpty(SellerUserName) || SellerClothing?.Length > 7)
        {
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            return State.RequestFailed;
        }

        var dto = await _bazaarEvents.Find(id, cancellationToken);
        if (dto == null)
        {
            _logger.LogWarning("Event with {Id} not found.", id);
            return State.EventNotFound;
        }

        BazaarNameAndDescription = dto.FormatEvent(new GermanDateTimeConverter());
        BazaarAddress = dto.Address;

        if (!dto.CanRegister)
        {
            return State.RegisterClosed;
        }

        return State.Success;
    }
}
