using GtKram.Core.Models;

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
}
