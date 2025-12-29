namespace GtKram.Infrastructure.Database.Repositories;

using GtKram.Infrastructure.Database.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

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
    private readonly ISqlRepository<Identity> _repository;

    public IdentityUserStore(
        IdentityErrorDescriber identityErrorDescriber,
        ISqlRepository<Identity> repository)
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
            if (user.Json.Claims.Contains(claim) == false)
            {
                user.Json.Claims.Add(claim);
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
        var entities = await _repository.SelectBy(0, e => e.Email, normalizedEmail, cancellationToken);
        return entities.Length == 1 ? entities[0] : default;
    }

    public async Task<Identity?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _repository.SelectOne(Guid.Parse(userId), cancellationToken);
    }

    public async Task<Identity?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectByJson(0, e => e.Json.UserName, normalizedUserName, cancellationToken);
        return entities.Length == 1 ? entities[0] : default;
    }

    public Task<int> GetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.AccessFailedCount);

    public Task<IList<Claim>> GetClaimsAsync(Identity user, CancellationToken cancellationToken)
    {
        var result = user.Json.Claims.Select(x => x.ToClaim()).ToArray();
        return Task.FromResult<IList<Claim>>(result ?? []);
    }

    public Task<string?> GetEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task<bool> GetEmailConfirmedAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.IsEmailConfirmed);

    public Task<bool> GetLockoutEnabledAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.IsLockoutEnabled);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.LockoutEnd);

    public Task<string?> GetNormalizedEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Email);

    public Task<string?> GetNormalizedUserNameAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(user.Json.UserName);

    public Task<string?> GetPasswordHashAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.PasswordHash);

    public Task<string?> GetSecurityStampAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.Json.SecurityStamp);

    public Task<string> GetUserIdAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult<string?>(user.Json.UserName);

    public async Task<IList<Identity>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectByJson(0, e => e.Json.Disabled, null, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var identityClaim = new IdentityClaim(claim);

        return [.. entities.Where(e => e.Json.Claims.Contains(identityClaim))];
    }

    public Task<bool> HasPasswordAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.Json.PasswordHash));

    public Task<int> IncrementAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(++user.Json.AccessFailedCount);

    public Task RemoveClaimsAsync(Identity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims.Select(claim => new IdentityClaim(claim)))
        {
            user.Json.Claims.Remove(claim);
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(Identity user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var userClaim = new IdentityClaim(claim);
        user.Json.Claims.Remove(userClaim);
        user.Json.Claims.Add(new IdentityClaim(newClaim));
        return Task.CompletedTask;
    }

    public Task ResetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken)
    {
        user.Json.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(Identity user, string? email, CancellationToken cancellationToken)
    {
        user.Json.Email = email!;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.Json.IsEmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        user.Json.IsLockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(Identity user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.Json.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(Identity user, string? normalizedEmail, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SetNormalizedUserNameAsync(Identity user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SetPasswordHashAsync(Identity user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.Json.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(Identity user, string stamp, CancellationToken cancellationToken)
    {
        user.Json.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(Identity user, string? userName, CancellationToken cancellationToken)
    {
        user.Json.UserName = userName!;
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
        if (!user.Json.Claims.Contains(claim))
        {
            user.Json.Claims.Add(claim);
        }
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        user.Json.Claims.Remove(new IdentityClaim(ClaimTypes.Role, roleName));
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(Identity user, CancellationToken cancellationToken)
    {
        var roles = user.Json.Claims.Where(c => c.Type == ClaimTypes.Role).Select(r => r.Value).ToArray();
        return Task.FromResult<IList<string>>(roles ?? []);
    }

    public Task<bool> IsInRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        var hasRole = user.Json.Claims.Contains(new IdentityClaim(ClaimTypes.Role, roleName));
        return Task.FromResult(hasRole);
    }

    public async Task<IList<Identity>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectByJson(0, e => e.Json.Disabled, null, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var roleClaim = new IdentityClaim(ClaimTypes.Role, roleName);

        return [.. entities.Where(e => e.Json.Claims.Contains(roleClaim))];
    }

    public Task SetAuthenticatorKeyAsync(Identity user, string key, CancellationToken cancellationToken)
    {
        user.Json.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Json.AuthenticatorKey);

    public Task SetTwoFactorEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        if (enabled)
        {
            if (!user.Json.Claims.Contains(IdentityClaim.TwoFactorClaim))
            {
                user.Json.Claims.Add(IdentityClaim.TwoFactorClaim);
            }
        }
        else
        {
            user.Json.Claims.Remove(IdentityClaim.TwoFactorClaim);
        }
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Json.Claims.Contains(IdentityClaim.TwoFactorClaim));
    }

    public Task SetPhoneNumberAsync(Identity user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.Json.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Json.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Json.IsPhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.Json.IsPhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }
}
