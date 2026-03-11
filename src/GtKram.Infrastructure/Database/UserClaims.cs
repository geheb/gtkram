using GtKram.Infrastructure.Database.Models;

namespace GtKram.Infrastructure.Database;

internal static class UserClaims
{
    public static readonly IdentityClaim TwoFactorClaim = new("2fa", "1");
}