using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.Users;

public sealed class CreateUserInput
{
    [Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    [Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [Display(Name = "Rollen")]
    [RequiredField]
    public bool[] Roles { get; set; } = new bool[5];

    public CreateUserCommand ToCommand(string callbackUrl)
    {
        var roles = new List<UserRoleType>();
        if (Roles[0]) roles.Add(UserRoleType.Administrator);
        if (Roles[1]) roles.Add(UserRoleType.Manager);
        if (Roles[2]) roles.Add(UserRoleType.Seller);
        if (Roles[3]) roles.Add(UserRoleType.Checkout);
        if (Roles[4]) roles.Add(UserRoleType.Helper);

        return new(Name!, Email!, [.. roles], callbackUrl);
    }
}
