using System.Security.Claims;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Database.Models;

internal sealed class IdentityClaim : IEquatable<IdentityClaim>, IEquatable<Claim>
{
    public static IdentityClaim TwoFactorClaim { get; } = new(UserClaims.TwoFactorClaim);

    public IdentityClaim(Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        Type = claim.Type;
        Value = claim.Value;
    }

    [JsonConstructor]
    public IdentityClaim(string type, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Type = type;
        Value = value;
    }

    public string Type { get; set; }

    public string Value { get; set; }

    public Claim ToClaim() => new(Type, Value);

    public bool Equals(IdentityClaim? other) =>
        other is not null &&
        other.Type.Equals(Type, StringComparison.Ordinal) &&
        other.Value.Equals(Value, StringComparison.Ordinal);

    public bool Equals(Claim? other) =>
        other is not null &&
        other.Type.Equals(Type, StringComparison.Ordinal) &&
        other.Value.Equals(Value, StringComparison.Ordinal);

    public override bool Equals(object? obj)
    {
        if (obj is IdentityClaim ic)
        {
            return Equals(ic);
        }
        return obj is Claim c && Equals(c);
    }

    public override int GetHashCode() => HashCode.Combine(Type, Value);
}
