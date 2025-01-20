using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IUserRepository
{
    Task<Result<User>> FindById(Guid id, CancellationToken cancellationToken);
    Task<Result<User>> FindByEmail(string email, CancellationToken cancellationToken);
    Task<User[]> GetAll(CancellationToken cancellationToken);
    Task<Result> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken);
    Task<Result> UpdateName(Guid id, string name, CancellationToken cancellationToken);
    Task<Result> UpdateRoles(Guid id, UserRoleType[] roles, CancellationToken cancellationToken);
    Task<Result> UpdateEmail(Guid id, string email, CancellationToken cancellationToken);
    Task<Result> UpdatePassword(Guid id, string password, CancellationToken cancellationToken);
    Task<Result> Disable(Guid id, CancellationToken cancellationToken);
}
