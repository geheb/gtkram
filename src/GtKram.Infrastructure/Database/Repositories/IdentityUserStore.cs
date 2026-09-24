namespace GtKram.Infrastructure.Database.Repositories;

using GtKram.Infrastructure.Database.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;

internal sealed class IdentityUserStore :
    IUserClaimStore<Identity>,
    IUserRoleStore<Identity>,
    IUserPasswordStore<Identity>,
    IUserSecurityStampStore<Identity>,
    IUserEmailStore<Identity>,
    IUserAuthenticatorKeyStore<Identity>,
    IUserTwoFactorStore<Identity>,
    IUserLockoutStore<Identity>,
    IUserPhoneNumberStore<Identity>
{
    private readonly IdentityErrorDescriber _identityErrorDescriber;
    private readonly ISqlRepository<Identity, IdentityValues> _repository;

    public IdentityUserStore(
        IdentityErrorDescriber identityErrorDescriber,
        ISqlRepository<Identity, IdentityValues> repository)
    {
        _identityErrorDescriber = identityErrorDescriber;
        _repository = repository;
    }

    public void Dispose()
    {
    }

    public Task AddClaimsAsync(Identity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims.Select(c => new IdentityClaim(c)))
        {
            if (user.Value.Claims.Contains(claim) == false)
            {
                user.Value.Claims.Add(claim);
            }
        }
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(Identity user, CancellationToken cancellationToken)
    {
        await _repository.Insert(user, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Identity user, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(user.Id, cancellationToken);
        if (affectedRows > 0)
        {
            return IdentityResult.Success;
        }
        return IdentityResult.Failed(_identityErrorDescriber.DefaultError());
    }

    public async Task<Identity?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field->>'{nameof(IdentityValues.NormalizedEmail)}'=@normalized_email
            """;
        var entities = await _repository.SelectBy(0, query, new { normalized_email = normalizedEmail }, cancellationToken);
        return entities.Length == 1 ? entities[0] : default;
    }

    public async Task<Identity?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _repository.SelectOne(Guid.Parse(userId), cancellationToken);
    }

    public async Task<Identity?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field->>'{nameof(IdentityValues.NormalizedUserName)}'=@normalized_username
            """;

        var entities = await _repository.SelectBy(0, query, new { normalized_username = normalizedUserName }, cancellationToken);
        return entities.Length == 1 ? entities[0] : default;
    }

    public Task<int> GetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.AccessFailedCount);

    public Task<IList<Claim>> GetClaimsAsync(Identity user, CancellationToken cancellationToken)
    {
        var result = user.Value.Claims.Select(x => x.ToClaim()).ToArray();
        return Task.FromResult<IList<Claim>>(result ?? []);
    }

    public Task<string?> GetEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Value.Email);

    public Task<bool> GetEmailConfirmedAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.IsEmailConfirmed);

    public Task<bool> GetLockoutEnabledAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.IsLockoutEnabled);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.LockoutEnd);

    public Task<string?> GetNormalizedEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Value.Email);

    public Task<string?> GetNormalizedUserNameAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Value.UserName);

    public Task<string?> GetPasswordHashAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.PasswordHash);

    public Task<string?> GetSecurityStampAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.Value.SecurityStamp);

    public Task<string> GetUserIdAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.Value.UserName);

    public async Task<IList<Identity>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field->>'{nameof(IdentityValues.Disabled)}' IS NULL
            """;

        var entities = await _repository.SelectBy(0, query, null, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var identityClaim = new IdentityClaim(claim);

        return [.. entities.Where(e => e.Value.Claims.Contains(identityClaim))];
    }

    public Task<bool> HasPasswordAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.Value.PasswordHash));

    public Task<int> IncrementAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(++user.Value.AccessFailedCount);

    public Task RemoveClaimsAsync(Identity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims.Select(claim => new IdentityClaim(claim)))
        {
            user.Value.Claims.Remove(claim);
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(Identity user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var userClaim = new IdentityClaim(claim);
        user.Value.Claims.Remove(userClaim);
        user.Value.Claims.Add(new IdentityClaim(newClaim));
        return Task.CompletedTask;
    }

    public Task ResetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken)
    {
        user.Value.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(Identity user, string? email, CancellationToken cancellationToken)
    {
        user.Value.Email = email!;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.Value.IsEmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        user.Value.IsLockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(Identity user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.Value.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(Identity user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.Value.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(Identity user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.Value.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(Identity user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.Value.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(Identity user, string stamp, CancellationToken cancellationToken)
    {
        user.Value.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(Identity user, string? userName, CancellationToken cancellationToken)
    {
        user.Value.UserName = userName!;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Identity user, CancellationToken cancellationToken)
    {
        var result = await _repository.Update(user, cancellationToken);
        return result ? IdentityResult.Success : IdentityResult.Failed(_identityErrorDescriber.ConcurrencyFailure());
    }

    public Task AddToRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        var claim = new IdentityClaim(ClaimTypes.Role, roleName);
        if (!user.Value.Claims.Contains(claim))
        {
            user.Value.Claims.Add(claim);
        }
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        user.Value.Claims.Remove(new IdentityClaim(ClaimTypes.Role, roleName));
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(Identity user, CancellationToken cancellationToken)
    {
        var roles = user.Value.Claims.Where(c => c.Type == ClaimTypes.Role).Select(r => r.Value).ToArray();
        return Task.FromResult<IList<string>>(roles ?? []);
    }

    public Task<bool> IsInRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        var hasRole = user.Value.Claims.Contains(new IdentityClaim(ClaimTypes.Role, roleName));
        return Task.FromResult(hasRole);
    }

    public async Task<IList<Identity>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field @> jsonb_build_object(
                '{nameof(IdentityValues.Claims)}', jsonb_build_array(jsonb_build_object(
                    '{nameof(IdentityClaim.Type)}', '{ClaimTypes.Role}',
                    '{nameof(IdentityClaim.Value)}', @claim_value)),
                '{nameof(IdentityValues.Disabled)}', null
            )
            """;

        return await _repository.SelectBy(0, query, new { claim_value = roleName }, cancellationToken);
    }

    public Task SetAuthenticatorKeyAsync(Identity user, string key, CancellationToken cancellationToken)
    {
        user.Value.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Value.AuthenticatorKey);

    public Task SetTwoFactorEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        var claim = UserClaims.TwoFactorClaim;
        if (enabled)
        {
            if (!user.Value.Claims.Contains(claim))
            {
                user.Value.Claims.Add(claim);
            }
        }
        else
        {
            user.Value.Claims.Remove(claim);
        }
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(Identity user, CancellationToken cancellationToken)
    {
        var claim = UserClaims.TwoFactorClaim;
        return Task.FromResult(user.Value.Claims.Contains(claim));
    }

    public Task SetPhoneNumberAsync(Identity user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.Value.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Value.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Value.IsPhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.Value.IsPhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }
}
