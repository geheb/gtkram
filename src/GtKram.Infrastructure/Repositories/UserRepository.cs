using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly PkGenerator _pkGenerator = new();
    private readonly TimeProvider _timeProvider;
    private readonly IRepository<Identity> _repo;
    private readonly UserManager<Identity> _userManager;
    private readonly IdentityErrorDescriber _errorDescriber;

    public UserRepository(
        TimeProvider timeProvider,
        IRepository<Identity> repo,
        UserManager<Identity> userManager,
        IdentityErrorDescriber errorDescriber)
    {
        _timeProvider = timeProvider;
        _repo = repo;
        _userManager = userManager;
        _errorDescriber = errorDescriber;
    }

    public async Task<Result<Guid>> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentOutOfRangeException.ThrowIfZero(roles.Length);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            var error = _errorDescriber.DuplicateEmail(email);
            return Result.Fail(error.Code, error.Description);
        }

        var id = _pkGenerator.Generate();

        var entity = new Identity
        {
            Id = id,
            Email = email,
            UserName = id.ToString().Replace("-", string.Empty),
            Name = name
        };

        var result = await _userManager.CreateAsync(entity);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        result = await _userManager.AddToRolesAsync(entity, roles.Select(r => r.MapToRole()));
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok(entity.Id);
    }

    public async Task<Result> Update(Guid id, string? newName, UserRoleType[]? newRoles, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(newName) && user.Name != newName)
        {
            user.Name = newName;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
            }
        }

        if (newRoles?.Length > 0)
        {
            var result = await MergeRoles(user, newRoles, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }

        return Result.Ok();
    }

    public async Task<Result> Disable(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        user.Email = user.UserName + "@disabled";
        user.PasswordHash = null;
        user.Name = new string(user.Name!.Split(' ').Select(u => u[0]).ToArray()) + "*";
        user.IsEmailConfirmed = false;
        user.Disabled = _timeProvider.GetUtcNow();
        user.LastLogin = null;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
    }

    public async Task<Result<Domain.Models.User>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        return Result.Ok(user.MapToDomain(_timeProvider.GetUtcNow(), new()));
    }

    public async Task<Result<Domain.Models.User>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        return Result.Ok(user.MapToDomain(_timeProvider.GetUtcNow(), new()));
    }

    public async Task<Domain.Models.User[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [
                new(static e => e.Disabled, null)
            ],
            cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        var now = _timeProvider.GetUtcNow();
        return entities.Select(e => e.Item.MapToDomain(now, dc)).OrderBy(e => e.Name).ToArray();
    }

    public async Task<Result> AddRole(Guid id, UserRoleType role, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var mappedRole = role.MapToRole();
        var currentStringRoles = await _userManager.GetRolesAsync(user);
        if (!currentStringRoles.Contains(mappedRole))
        {
            var result = await _userManager.AddToRoleAsync(user, mappedRole);
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    private async Task<Result> MergeRoles(Identity user, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        IdentityResult result;
        var currentStringRoles = await _userManager.GetRolesAsync(user);
        if (currentStringRoles.Count == 0)
        {
            result = await _userManager.AddToRolesAsync(user, roles.Select(r => r.MapToRole()));
            return result.Succeeded ? Result.Ok() : Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        var currentRoles = currentStringRoles.Select(r => r.MapToRole()).ToArray();
        var removeRoles = currentRoles.Except(roles).ToArray();
        var addRoles = roles.Except(currentRoles).ToArray();

        if (removeRoles.Length > 0)
        {
            result = await _userManager.RemoveFromRolesAsync(user, removeRoles.Select(r => r.MapToRole()));
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
            }
        }

        if (addRoles.Length > 0)
        {
            result = await _userManager.AddToRolesAsync(user, addRoles.Select(r => r.MapToRole()));
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
            }
        }

        return Result.Ok();
    }
}
