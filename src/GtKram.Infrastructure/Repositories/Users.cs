using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using System.Security.Claims;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Users : IUsers
{
    private readonly TimeProvider _timeProvider;
    private readonly ISqlRepository<Identity> _repository;
    private readonly UserManager<Identity> _userManager;
    private readonly IdentityErrorDescriber _errorDescriber;

    public Users(
        TimeProvider timeProvider,
        ISqlRepository<Identity> repository,
        UserManager<Identity> userManager,
        IdentityErrorDescriber errorDescriber)
    {
        _timeProvider = timeProvider;
        _repository = repository;
        _userManager = userManager;
        _errorDescriber = errorDescriber;
    }

    public async Task<ErrorOr<Guid>> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.Email, email, cancellationToken);
        if (entities.Length > 0)
        {
            var error = _errorDescriber.DuplicateEmail(email);
            return Error.Failure(error.Code, error.Description);
        }

        var entity = new Identity
        {
            Json = new()
            {
                Email = email,
                UserName = Guid.NewGuid().ToString("N"),
                Name = name,
            },
        };

        entity.Json.Claims.AddRange(roles.Select(r => new IdentityClaim(ClaimTypes.Role, r.MapToRole())));

        var result = await _userManager.CreateAsync(entity);

        return entity.Id;
    }

    public async Task<ErrorOr<Success>> AddRoles(Guid id, UserRoleType[] roles, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        foreach (var role in roles)
        {
            var roleClaim = new IdentityClaim(ClaimTypes.Role, role.MapToRole());
            if (!entity.Json.Claims.Contains(roleClaim))
            {
                entity.Json.Claims.Add(roleClaim);
            }
        }

        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Error.Failure(Domain.Errors.Internal.ConflictData.Code);
    }

    public async Task<ErrorOr<Success>> Update(Guid id, string? newName, UserRoleType[]? newRoles, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        if (!string.IsNullOrWhiteSpace(newName) && entity.Json.Name != newName)
        {
            entity.Json.Name = newName;
        }

        if (newRoles?.Length > 0)
        {
            entity.Json.Claims.RemoveAll(c => c.Type == ClaimTypes.Role);
            foreach (var role in newRoles)
            {
                entity.Json.Claims.Add(new IdentityClaim(ClaimTypes.Role, role.MapToRole()));
            }
        }

        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Error.Failure(Domain.Errors.Internal.ConflictData.Code);
    }

    public async Task<ErrorOr<Success>> Disable(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var name = new string([.. entity.Json.Name!.Split(' ').Select(u => u[0])]);

        entity.Json.Email = entity.Json.UserName + "@disabled";
        entity.Json.PasswordHash = null;
        entity.Json.Name = name;
        entity.Json.IsEmailConfirmed = false;
        entity.Json.Disabled = _timeProvider.GetUtcNow();
        entity.Json.LastLogin = null;
        entity.Json.PhoneNumber = null;
        entity.Json.IsPhoneNumberConfirmed = false;
        entity.Json.AuthenticatorKey = null;
        entity.Json.LockoutEnd = null;
        entity.Json.Claims.Clear();

        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Error.Failure(Domain.Errors.Internal.ConflictData.Code);
    }

    public async Task<User[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectByJson(0, e => e.Json.Disabled, null, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        var now = _timeProvider.GetUtcNow();
        return entities.Select(e => e.MapToDomain(now, dc)).OrderBy(e => e.Name).ToArray();
    }

    public async Task<ErrorOr<User>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var entity = await _repository.SelectBy(0, e => e.Email, email, cancellationToken);
        if (entity.Length == 0)
        {
            return Domain.Errors.Identity.NotFound;
        }

        return entity[0].MapToDomain(_timeProvider.GetUtcNow(), new());
    }

    public async Task<ErrorOr<User>> FindById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        return entity.MapToDomain(_timeProvider.GetUtcNow(), new());
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _repository.Delete(id, cancellationToken);
            return result > 0 ? Result.Success : Domain.Errors.Identity.DeleteFailed;
        }
        catch (SqliteException)
        {
            return Domain.Errors.Identity.DeleteFailed;
        }
    }
}
