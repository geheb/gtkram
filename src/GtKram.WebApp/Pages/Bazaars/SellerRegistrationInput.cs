using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.Bazaars;

public sealed class SellerRegistrationInput
{
    public string State_Event { get; set; } = "Unbekannt";

    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    public string? Email { get; set; }

    [Display(Name = "Telefonnummer")]
    public string? Phone { get; set; }

    // see also BazaarEventInput.MaxSellers
    [Display(Name = "Verkäufernummer")]
    [RequiredField, RangeField(1, 200)]
    public int SellerNumber { get; set; }

    [Display(Name = "Rolle des Verkäufers")]
    [RequiredField, RangeField((int)SellerRole.Standard, (int)SellerRole.Orga)]
    public int Role { get; set; }

    [Display(Name = "Darf kassieren")]
    [RequiredField]
    public bool CanCheckout { get; set; }

    [Display(Name = "Max. Artikel")]
    [RangeField(1, 999)]
    public int? MaxArticleCount { get; set; }

    internal void InitDefault(SellerRegistration registration)
    {
        Name = registration.Name;
        Email = registration.Email;
        Phone = registration.Phone;
    }

    internal void Init(Seller seller)
    {
        SellerNumber = seller.SellerNumber;
        Role = (int)seller.Role;
        CanCheckout = seller.CanCheckout;
        MaxArticleCount = seller.MaxArticleCount;
    }

    internal UpdateSellerCommand ToCommand(Guid id) =>
        new(id, SellerNumber, (SellerRole)Role, CanCheckout, MaxArticleCount);
}
