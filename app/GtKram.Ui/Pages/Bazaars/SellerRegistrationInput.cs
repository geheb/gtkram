using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Extensions;
using GtKram.Domain.Models;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

public class SellerRegistrationInput
{
    public string State_Event { get; set; } = "Unbekannt";
    public Guid? State_SellerId { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    public string? Email { get; set; }

    [Display(Name = "Telefonnummer")]
    public string? Phone { get; set; }

    // see also BazaarEventInput.MaxSellers
    [Display(Name = "Verkäufernummer")]
    [RequiredField, Range(0, 200, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int SellerNumber { get; set; }

    [Display(Name = "Rolle des Verkäufers")]
    [RequiredField, Range((int)SellerRole.Standard, (int)SellerRole.Orga, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int Role { get; set; }

    [Display(Name = "Darf kassieren")]
    [RequiredField]
    public bool CanCreateBillings { get; set; }

    internal void InitDefault(BazaarSellerRegistration registration)
    {
        Name = registration.Name;
        Email = registration.Email;
        Phone = registration.Phone;
    }

    internal void Init(BazaarSeller seller)
    {
        SellerNumber = seller.SellerNumber;
        Role = (int)seller.Role;
        CanCreateBillings = seller.CanCreateBillings;
    }

    internal UpdateSellerCommand ToCommand(Guid id)
    {
        return new(new()
        {
            Id = id,
            SellerNumber = SellerNumber,
            Role = (SellerRole)Role,
            CanCreateBillings = CanCreateBillings,
            MaxArticleCount = ((SellerRole)Role).GetMaxArticleCount()
        });
    }
}
