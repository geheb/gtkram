using System.Security.Claims;

namespace GtKram.Infrastructure.Database;

internal static class UserClaims
{
    public static readonly Claim TwoFactorClaim = new("2fa", "1");
}