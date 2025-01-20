using Microsoft.AspNetCore.Identity;

namespace GtKram.Infrastructure.Persistence.Entities;

public sealed class IdentityUserRoleGuid : IdentityUserRole<Guid> // ApplicationUserRole 
{
    public IdentityUserGuid? User { get; set; }
    public IdentityRoleGuid? Role { get; set; }
}
