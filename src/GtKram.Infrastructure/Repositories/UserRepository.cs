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

    public async Task<Result> UpdateEmail(Guid id, string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        if (user.Email == email)
        {
            return Result.Ok();
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, email);
        var result = await _userManager.ChangeEmailAsync(user, email, token);
        if (!result.Succeeded)
        {
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok();
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
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok();
    }

    public async Task<Result> UpdatePassword(Guid id, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        IdentityResult result;
        foreach (var validator in _userManager.PasswordValidators)
        {
            result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
            {
                return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
            }
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
        }

        if (!user.EmailConfirmed)
        {
            token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
            }
        }

        return Result.Ok();
    }

    public async Task<Result> UpdateRoles(Guid id, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(roles.Length);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

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
                return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
            }
        }

        if (addRoles.Length > 0)
        {
            result = await _userManager.AddToRolesAsync(user, addRoles.Select(r => r.MapToRole()));
            if (!result.Succeeded)
            {
                return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => e.Description));
            }
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
}
