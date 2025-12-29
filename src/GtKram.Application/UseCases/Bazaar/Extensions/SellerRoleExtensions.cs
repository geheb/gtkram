using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Extensions;

public static class SellerRoleExtensions
{
    public static int GetMaxArticleCount(this SellerRole role) => role switch
    {
        SellerRole.Orga => 20 * 24,
        SellerRole.TeamLead => 4 * 24,
        SellerRole.Helper => 3 * 24,
        _ => 2 * 24
    };
}
