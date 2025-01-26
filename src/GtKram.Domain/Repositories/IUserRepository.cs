using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IUserRepository
{
    Task<Result<User>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result<User>> Find(string email, CancellationToken cancellationToken);
    Task<User[]> GetAll(CancellationToken cancellationToken);
    Task<Result<Guid>> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken);
    Task<Result> Update(Guid id, string? newName, UserRoleType[]? newRoles, CancellationToken cancellationToken);
    Task<Result> Disable(Guid id, CancellationToken cancellationToken);
    Task<Result> AddRole(Guid id, UserRoleType role, CancellationToken cancellationToken);
}
