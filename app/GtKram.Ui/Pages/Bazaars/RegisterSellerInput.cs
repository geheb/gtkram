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

    public bool[] SellerClothing { get; set; } = new bool[6];

    public bool HasKita { get; set; }

    [Display(Name = "Die goldenen Regeln gelesen")]
    [RequireTrueField]
    public bool HasGoldenRules { get; set; }

    internal CreateSellerRegistrationCommand ToCommand(Guid eventId)
    {
        var clothingType = new List<int>();
        if (SellerClothing[0]) clothingType.Add(0);
        if (SellerClothing[1]) clothingType.Add(1);
        if (SellerClothing[2]) clothingType.Add(2);
        if (SellerClothing[3]) clothingType.Add(3);
        if (SellerClothing[4]) clothingType.Add(4);
        if (SellerClothing[5]) clothingType.Add(5);

        return new(new()
        {
            EventId = eventId,
            Email = SellerEmail!,
            Name = SellerName!,
            Phone = SellerPhone!,
            ClothingType = clothingType.Count > 0 ? [.. clothingType] : null,
            PreferredType = HasKita ? SellerRegistrationPreferredType.Kita : SellerRegistrationPreferredType.None,
        }, true);
    }
}
