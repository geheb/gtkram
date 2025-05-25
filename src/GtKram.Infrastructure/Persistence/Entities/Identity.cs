using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("identities")]
internal sealed class Identity : IEntity
{
    public required Guid Id { get; set; }

    public required string Email { get; set; }

    public required string UserName { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset? LastLogin { get; set; }

    public DateTimeOffset? Disabled { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool IsLockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public ICollection<IdentityClaim> Claims { get; set; } = [];

    public string? AuthenticatorKey { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsPhoneNumberConfirmed { get; set; }

    [JsonIgnore]
    public int Version {  get; set; }
}
