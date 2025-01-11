using System.Security.Claims;

namespace GtKram.Core.User;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetId(this ClaimsPrincipal principal)
    {
        var name = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(name, out var id) ? id : Guid.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        return email!;
    }
}
