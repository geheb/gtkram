using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Models;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Users;

public class UpdateUserInput
{
    [Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [Display(Name = "Passwort")]
    [PasswordLengthField]
    public string? Password { get; set; }

    [Display(Name = "Rollen")]
    [RequiredField]
    public bool[] Roles { get; set; } = new bool[4];

    public void Init(User user)
    {
        Name = user.Name;
        Email = user.Email;
        Roles[0] = user.Roles.Contains(UserRoleType.Administrator);
        Roles[1] = user.Roles.Contains(UserRoleType.Manager);
        Roles[2] = user.Roles.Contains(UserRoleType.Seller);
        Roles[3] = user.Roles.Contains(UserRoleType.Checkout);
    }

    public UpdateUserCommand ToCommand(Guid id)
    {
        var roles = new List<UserRoleType>();
        if (Roles[0]) roles.Add(UserRoleType.Administrator);
        if (Roles[1]) roles.Add(UserRoleType.Manager);
        if (Roles[2]) roles.Add(UserRoleType.Seller);
        if (Roles[3]) roles.Add(UserRoleType.Checkout);

        return new(id, Name, [.. roles]);
    }
}
