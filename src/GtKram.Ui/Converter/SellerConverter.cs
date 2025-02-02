using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public sealed class SellerConverter
{
    public string RoleToString(SellerRole? role)
    {
        if (!role.HasValue) return string.Empty;
        return role.Value switch
        {
            SellerRole.Standard => "Verkäufer",
            SellerRole.Helper => "Helfer",
            SellerRole.Orga => "Organisation",
            SellerRole.TeamLead => "Teamleiter",
            _ => $"Unbekannt: {role}"
        };
    }

    public string RoleToClass(SellerRole? role)
    {
        if (!role.HasValue) return string.Empty;
        return role.Value switch
        {
            SellerRole.Standard => "is-info",
            SellerRole.Helper => "is-success",
            SellerRole.Orga => "is-danger",
            SellerRole.TeamLead => "is-warning",
            _ => string.Empty
        };
    }
}
