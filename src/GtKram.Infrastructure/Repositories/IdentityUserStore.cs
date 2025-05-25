namespace GtKram.Infrastructure.Repositories;

using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.User;
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
    private readonly IRepository<Identity> _repo;

    public IdentityUserStore(
        IdentityErrorDescriber identityErrorDescriber,
        IRepository<Identity> repo)
    {
        _identityErrorDescriber = identityErrorDescriber;
        _repo = repo;
    }

    public void Dispose()
    {
    }

    public Task AddClaimsAsync(Identity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims.Select(c => new IdentityClaim(c)))
        {
            if (!user.Claims.Contains(claim))
            {
                user.Claims.Add(claim);
            }
        }
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(Identity user, CancellationToken cancellationToken)
    {
        await _repo.Create(user, null, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Identity user, CancellationToken cancellationToken)
    {
        var affectedRows = await _repo.Delete(user.Id, null, cancellationToken);
        if (affectedRows > 0)
        {
            return IdentityResult.Success;
        }
        return IdentityResult.Failed(_identityErrorDescriber.DefaultError());
    }

    public async Task<Identity?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.Email, normalizedEmail)],
            null,
            cancellationToken);

        return entities.Any() ? Map(entities[0]) : default;
    }

    public async Task<Identity?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(Guid.Parse(userId), null, cancellationToken);
        return entity is null ? default : Map(entity.Value);
    }

    public async Task<Identity?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.UserName, normalizedUserName)],
            null,
            cancellationToken);

        return entities.Any() ? Map(entities[0]) : default;
    }

    public Task<int> GetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<IList<Claim>> GetClaimsAsync(Identity user, CancellationToken cancellationToken)
    {
        var result = user.Claims.Select(x => x.ToClaim()).ToList();
        return Task.FromResult<IList<Claim>>(result);
    }

    public Task<string?> GetEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email ?? null);

    public Task<bool> GetEmailConfirmedAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.IsEmailConfirmed);

    public Task<bool> GetLockoutEnabledAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.IsLockoutEnabled);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task<string?> GetNormalizedEmailAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email ?? null);

    public Task<string?> GetNormalizedUserNameAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName ?? null);

    public Task<string?> GetPasswordHashAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<string?> GetSecurityStampAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    public Task<string> GetUserIdAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(Identity user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName ?? null);

    public async Task<IList<Identity>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.Disabled, null)],
            null,
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var identityClaim = new IdentityClaim(claim);

        return [.. entities.Where(e => e.Item.Claims.Contains(identityClaim)).Select(Map)];
    }

    public Task<bool> HasPasswordAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));

    public Task<int> IncrementAccessFailedCountAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(++user.AccessFailedCount);

    public Task RemoveClaimsAsync(Identity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims.Select(claim => new IdentityClaim(claim)))
        {
            user.Claims.Remove(claim);
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(Identity user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var userClaim = new IdentityClaim(claim);
        user.Claims.Remove(userClaim);
        user.Claims.Add(new IdentityClaim(newClaim));
        return Task.CompletedTask;
    }

    public Task ResetAccessFailedCountAsync(Identity user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(Identity user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email!;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.IsEmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetLockoutEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        user.IsLockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetLockoutEndDateAsync(Identity user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(Identity user, string? normalizedEmail, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SetNormalizedUserNameAsync(Identity user, string? normalizedName, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SetPasswordHashAsync(Identity user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(Identity user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(Identity user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName!;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Identity user, CancellationToken cancellationToken)
    {
        var result = await _repo.Update(user, null, cancellationToken);
        if (result == UpdateResult.Success)
        {
            return IdentityResult.Success;
        }
        else if (result == UpdateResult.Conflict)
        {
            return IdentityResult.Failed(_identityErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Failed(_identityErrorDescriber.DefaultError());
    }

    public Task AddToRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        var claim = new IdentityClaim(ClaimsIdentity.DefaultRoleClaimType, roleName);
        if (!user.Claims.Contains(claim))
        {
            user.Claims.Add(claim);
        }
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        user.Claims.Remove(new IdentityClaim(ClaimsIdentity.DefaultRoleClaimType, roleName));
        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(Identity user, CancellationToken cancellationToken)
    {
        var roles = user.Claims.Where(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).Select(r => r.Value).ToList();
        return Task.FromResult<IList<string>>(roles);
    }

    public Task<bool> IsInRoleAsync(Identity user, string roleName, CancellationToken cancellationToken)
    {
        var hasRole = user.Claims.Contains(new IdentityClaim(ClaimsIdentity.DefaultRoleClaimType, roleName));
        return Task.FromResult(hasRole);
    }

    public async Task<IList<Identity>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.Disabled, null)],
            null,
            cancellationToken);

        var identityClaim = new IdentityClaim(ClaimsIdentity.DefaultRoleClaimType, roleName);

        return [.. entities.Where(e => e.Item.Claims.Contains(identityClaim)).Select(Map)];
    }

    public Task SetAuthenticatorKeyAsync(Identity user, string key, CancellationToken cancellationToken)
    {
        user.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(Identity user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AuthenticatorKey);

    public Task SetTwoFactorEnabledAsync(Identity user, bool enabled, CancellationToken cancellationToken)
    {
        if (enabled)
        {
            user.Claims.Add(IdentityClaim.TwoFactorClaim);
        }
        else
        {
            user.Claims.Remove(IdentityClaim.TwoFactorClaim);
        }
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Claims.Contains(IdentityClaim.TwoFactorClaim));
    }

    private static Identity Map(Entity<Identity> entity) => entity.Item;

    public Task SetPhoneNumberAsync(Identity user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(Identity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.IsPhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(Identity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.IsPhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }
}
