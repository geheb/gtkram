using Microsoft.AspNetCore.Identity;

namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class IdentityRoleGuid : IdentityRole<Guid> // ApplicationRole
{
    public ICollection<IdentityUserRoleGuid>? UserRoles { get; set; }
}
