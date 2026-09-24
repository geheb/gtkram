using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Users : IUsers
{
    private readonly TimeProvider _timeProvider;
    private readonly ISqlRepository<Identity, IdentityValues> _repository;
    private readonly UserManager<Identity> _userManager;
    private readonly IdentityErrorDescriber _errorDescriber;

    public Users(
        TimeProvider timeProvider,
        ISqlRepository<Identity, IdentityValues> repository,
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
        var normalizedEmail = _userManager.NormalizeEmail(email);

        const string query = $"""
            $data_field->>'{nameof(IdentityValues.NormalizedEmail)}' = @normalized_email
            """;

        var entities = await _repository.SelectBy(0, query, new { normalized_email = normalizedEmail }, cancellationToken);
        if (entities.Length > 0)
        {
            var error = _errorDescriber.DuplicateEmail(email);
            return Error.Failure(error.Code, error.Description);
        }

        var entity = new Identity
        {
            Id = Guid.CreateVersion7(),
            Value = new()
            {
                Email = email,
                UserName = Guid.NewGuid().ToString("N"),
                Name = name,
            },
        };

        entity.Value.Claims.AddRange(roles.Select(r => new IdentityClaim(ClaimTypes.Role, r.MapToRole())));

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
            if (!entity.Value.Claims.Contains(roleClaim))
            {
                entity.Value.Claims.Add(roleClaim);
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

        if (!string.IsNullOrWhiteSpace(newName) && entity.Value.Name != newName)
        {
            entity.Value.Name = newName;
        }

        if (newRoles?.Length > 0)
        {
            entity.Value.Claims.RemoveAll(c => c.Type == ClaimTypes.Role);
            foreach (var role in newRoles)
            {
                entity.Value.Claims.Add(new IdentityClaim(ClaimTypes.Role, role.MapToRole()));
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

        var name = new string([.. entity.Value.Name!.Split(' ').Select(u => u[0])]);

        entity.Value.Email = entity.Value.UserName + "@disabled";
        entity.Value.NormalizedEmail = entity.Value.Email;
        entity.Value.PasswordHash = null;
        entity.Value.Name = name;
        entity.Value.IsEmailConfirmed = false;
        entity.Value.Disabled = _timeProvider.GetUtcNow();
        entity.Value.LastLogin = null;
        entity.Value.PhoneNumber = null;
        entity.Value.IsPhoneNumberConfirmed = false;
        entity.Value.AuthenticatorKey = null;
        entity.Value.LockoutEnd = null;
        entity.Value.Claims.Clear();

        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Error.Failure(Domain.Errors.Internal.ConflictData.Code);
    }

    public async Task<User[]> GetAll(CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field->>'{nameof(IdentityValues.Disabled)}' IS NULL
            """;

        var entities = await _repository.SelectBy(0, query, null, cancellationToken);

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
        const string query = $"""
            $data_field->>'{nameof(IdentityValues.NormalizedEmail)}' = @normalized_email
            """;

        var normalizedEmail = _userManager.NormalizeEmail(email);

        var entity = await _repository.SelectBy(0, query, new { normalized_email = normalizedEmail }, cancellationToken);
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
        var result = await _repository.Delete(id, cancellationToken);
        return result > 0 ? Result.Success : Domain.Errors.Identity.DeleteFailed;
    }
}
