using GtKram.Application.UseCases.User.Models;
using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public class RoleConverter
{
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
