namespace GtKram.Domain.Models;

public sealed class User
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required UserRoleType[] Roles { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public DateTimeOffset? LockoutEndDate { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
}
