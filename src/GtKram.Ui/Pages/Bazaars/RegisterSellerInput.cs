using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Models;
using GtKram.Ui.Annotations;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

public sealed class RegisterSellerInput
{
    public string State_Event { get; set; } = "Unbekannt";
    public string? State_Address { get; set; }

    public string? SellerUserName { get; set; } // just for bots

    [Display(Name = "Name")]
    [RequiredField, StringLength(64, MinimumLength = 2, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? SellerName { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? SellerEmail { get; set; }

    [Display(Name = "Telefonnummer")]
    [RequiredField, RegularExpression(@"^(\d{4,15})$", ErrorMessage = "Das Feld '{0}' muss zwischen 4 und 15 Zeichen liegen und darf nur Zahlen enthalten.")]
    public string? SellerPhone { get; set; }

    public int[] SellerClothing { get; set; } = [];

    public bool HasKita { get; set; }

    [Display(Name = "Die goldenen Regeln gelesen")]
    [RequireTrueField]
    public bool HasGoldenRules { get; set; }

    internal CreateSellerRegistrationCommand ToCommand(Guid eventId) => new(new()
    {
        BazaarEventId = eventId,
        Email = SellerEmail!,
        Name = SellerName!,
        Phone = SellerPhone!,
        ClothingType = SellerClothing,
        PreferredType = HasKita ? SellerRegistrationPreferredType.Kita : SellerRegistrationPreferredType.None,
    }, true);
}
