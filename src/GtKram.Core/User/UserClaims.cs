using System.Security.Claims;

namespace GtKram.Core.User;

public static class UserClaims
{
    public static readonly Claim TwoFactorClaim = new("2fa", "1");
}