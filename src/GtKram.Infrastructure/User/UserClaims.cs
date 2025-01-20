using System.Security.Claims;

namespace GtKram.Infrastructure.User;

internal static class UserClaims
{
    public static readonly Claim TwoFactorClaim = new("2fa", "1");
}