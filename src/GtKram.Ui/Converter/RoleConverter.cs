using GtKram.Application.UseCases.User.Models;
using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public class RoleConverter
{
    public string RoleToString(string role)
    {
        return role switch
        {
            Roles.Admin => "Administrator",
            Roles.Manager => "Manager",
            Roles.Seller => "Verkäufer",
            Roles.Billing => "Kassierer",
            _ => string.Empty
        };
    }

    public string RoleToString(UserRoleType role)
    {
        return role switch
        {
            UserRoleType.Administrator => "Administrator",
            UserRoleType.Manager => "Manager",
            UserRoleType.Seller => "Verkäufer",
            UserRoleType.Billing => "Kassierer",
            _ => string.Empty
        };
    }

    public string RoleToClass(string role)
    {
        return role switch
        {
            Roles.Admin => "is-danger",
            Roles.Manager => "is-warning",
            Roles.Seller => "is-info",
            Roles.Billing => "is-success",
            _ => string.Empty
        };
    }

    public string RoleToClass(UserRoleType role)
    {
        return role switch
        {
            UserRoleType.Administrator => "is-danger",
            UserRoleType.Manager => "is-warning",
            UserRoleType.Seller => "is-info",
            UserRoleType.Billing => "is-success",
            _ => string.Empty
        };
    }
}
