using Microsoft.AspNetCore.Identity;

namespace GtKram.Infrastructure.Persistence.Entities;

public sealed class IdentityUserGuid : IdentityUser<Guid> // ApplicationUser
{
    public string? Name { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset? DisabledOn { get; set; }

    public ICollection<IdentityUserRoleGuid>? UserRoles { get; set; }
    internal ICollection<AccountNotification>? AccountNotifications { get; set; }
    internal ICollection<BazaarSeller>? BazaarSellers { get; set; }
    internal ICollection<BazaarBilling>? BazaarBillings { get; set; }
}
