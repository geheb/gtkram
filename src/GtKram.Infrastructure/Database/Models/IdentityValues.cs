namespace GtKram.Infrastructure.Database.Models;

internal sealed class IdentityValues
{
    public string Email { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public bool IsEmailConfirmed { get; set; }

    public string Name { get; set; } = null!;

    public DateTimeOffset? LastLogin { get; set; }

    public DateTimeOffset? Disabled { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool IsLockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public List<IdentityClaim> Claims { get; set; } = [];

    public string? AuthenticatorKey { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsPhoneNumberConfirmed { get; set; }
}
