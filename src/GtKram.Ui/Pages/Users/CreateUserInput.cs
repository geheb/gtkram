using GtKram.Application.UseCases.User.Models;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Users;

public class CreateUserInput
{
    [Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [Display(Name = "Rollen")]
    [RequiredField]
    public bool[] Roles { get; set; } = new bool[4];

    public void To(UserDto dto)
    {
        dto.Name = Name;
        dto.Email = Email;

        var roles = new List<string>();
        if (Roles[0]) roles.Add(Application.UseCases.User.Models.Roles.Admin);
        if (Roles[1]) roles.Add(Application.UseCases.User.Models.Roles.Manager);
        if (Roles[2]) roles.Add(Application.UseCases.User.Models.Roles.Seller);
        if (Roles[3]) roles.Add(Application.UseCases.User.Models.Roles.Billing);
        dto.Roles = roles.ToArray();
    }
}
