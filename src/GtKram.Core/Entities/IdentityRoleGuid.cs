using Microsoft.AspNetCore.Identity;

namespace GtKram.Core.Entities;

public sealed class IdentityRoleGuid : IdentityRole<Guid> // ApplicationRole
{
    public ICollection<IdentityUserRoleGuid>? UserRoles { get; set; }
}
