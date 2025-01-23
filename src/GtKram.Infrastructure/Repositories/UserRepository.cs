using FluentResults;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private const string _userNotFound = "Der Benutzer wurde nicht gefunden.";
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly TimeProvider _timeProvider;
    private readonly UserManager<IdentityUserGuid> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly IdentityErrorDescriber _errorDescriber;

    public UserRepository(
        TimeProvider timeProvider,
        UserManager<IdentityUserGuid> userManager,
        AppDbContext dbContext,
        IdentityErrorDescriber errorDescriber)
    {
        _timeProvider = timeProvider;
        _userManager = userManager;
        _dbContext = dbContext;
        _errorDescriber = errorDescriber;
    }

    public async Task<Result> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentOutOfRangeException.ThrowIfZero(roles.Length);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return Result.Fail(_errorDescriber.DuplicateEmail(email).Description);
        }

        var entity = new IdentityUserGuid()
        {
            Id = _pkGenerator.Generate(),
            UserName = Guid.NewGuid().ToString().Replace("-", string.Empty),
            Email = email,
            Name = name
        };

        var result = await _userManager.CreateAsync(entity);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        result = await _userManager.AddToRolesAsync(entity, roles.Select(r => r.MapToRole()));
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok();
    }

    public async Task<Result> Update(Guid id, string newName, string newEmail, string? newPassword, UserRoleType[] newRoles, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        IdentityResult identityResult;

        if (!string.IsNullOrWhiteSpace(newName) && user.Name != newName)
        {
            user.Name = newName;
            identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.Errors.Select(e => e.Description));
            }
        }

        var result = await UpdateRoles(user, newRoles, cancellationToken);
        if (result.IsFailed)
        {
            return result;
        }

        var shouldConfirmEmail = false;

        if (!string.IsNullOrWhiteSpace(newEmail) && user.Email != newEmail)
        {
            var hasFound = await _userManager.FindByEmailAsync(newEmail) is not null;
            if (hasFound)
            {
                return Result.Fail("Die neue E-Mail-Adresse ist bereits vergeben.");
            }
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            identityResult = await _userManager.ChangeEmailAsync(user, newEmail, token);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.Errors.Select(e => e.Description));
            }
            shouldConfirmEmail = true;
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            identityResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.Errors.Select(e => e.Description));
            }
            shouldConfirmEmail = true;
        }

        if (!user.EmailConfirmed && shouldConfirmEmail)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            identityResult = await _userManager.ConfirmEmailAsync(user, token);
            if (!identityResult.Succeeded)
            {
                return Result.Fail(identityResult.Errors.Select(e => e.Description));
            }
        }

        return Result.Ok();
    }

    public async Task<Result> Disable(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        user.Email = $"{user.UserName}@deactivated";
        user.PasswordHash = null;
        user.Name = new string(user.Name!.Split(' ').Select(u => u[0]).ToArray()) + "*";
        user.EmailConfirmed = false;
        user.DisabledOn = _timeProvider.GetUtcNow();
        user.LastLogin = null;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
    }

    public async Task<Result<Domain.Models.User>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        return Result.Ok(user.MapToDomain(new()));
    }

    public async Task<Result<Domain.Models.User>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }
        return Result.Ok(user.MapToDomain(new()));
    }

    public async Task<Domain.Models.User[]> GetAll(CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<IdentityUserGuid>();
        var result = await dbSet
            .Include(e => e.UserRoles)
            .Where(e => e.DisabledOn == null)
            .ToArrayAsync(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return result.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Result> UpdateName(Guid id, string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        if (user.Name == name)
        {
            return Result.Ok();
        }

        user.Name = name;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok();
    }

    public async Task<Result> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, currentPassword);
        if (result != PasswordVerificationResult.Success)
        {
            return Result.Fail("Das aktuelle Passwort stimmt nicht überein.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var identityResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!identityResult.Succeeded)
        {
            return Result.Fail(identityResult.Errors.Select(e => e.Description));
        }

        return Result.Ok();
    }

    public async Task<Result<string>> CreateEmailConfirmationToken(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Fail("Die E-Mail-Adresse wurde bereits bestätigt.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return Result.Ok(token);
    }

    public async Task<Result<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        return Result.Ok(token);
    }

    public async Task<Result> VerifyPassword(Guid id, string password, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (result != PasswordVerificationResult.Success)
        {
            return Result.Fail("Das angegebene Passwort stimmt nicht überein.");
        }

        return Result.Ok();
    }

    private async Task<Result> UpdateRoles(IdentityUserGuid user, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        IdentityResult result;
        var currentStringRoles = await _userManager.GetRolesAsync(user);
        if (currentStringRoles.Count == 0)
        {
            result = await _userManager.AddToRolesAsync(user, roles.Select(r => r.MapToRole()));
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
        }

        var currentRoles = currentStringRoles.Select(r => r.MapToRole()).ToArray();
        var removeRoles = currentRoles.Except(roles).ToArray();
        var addRoles = roles.Except(currentRoles).ToArray();

        if (removeRoles.Length > 0)
        {
            result = await _userManager.RemoveFromRolesAsync(user, removeRoles.Select(r => r.MapToRole()));
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => e.Description));
            }
        }

        if (addRoles.Length > 0)
        {
            result = await _userManager.AddToRolesAsync(user, addRoles.Select(r => r.MapToRole()));
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => e.Description));
            }
        }

        return Result.Ok();
    }
}
