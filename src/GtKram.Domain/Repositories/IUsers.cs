using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IUsers
{
    Task<ErrorOr<User>> FindById(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<User>> FindByEmail(string email, CancellationToken cancellationToken);
    Task<User[]> GetAll(CancellationToken cancellationToken);
    Task<ErrorOr<Guid>> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Guid id, string? newName, UserRoleType[]? newRoles, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Disable(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> AddRoles(Guid id, UserRoleType[] roles, CancellationToken cancellationToken);
}
