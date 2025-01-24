using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IUserRepository
{
    Task<Result<User>> FindById(Guid id, CancellationToken cancellationToken);
    Task<Result<User>> FindByEmail(string email, CancellationToken cancellationToken);
    Task<User[]> GetAll(CancellationToken cancellationToken);
    Task<Result> Create(string name, string email, UserRoleType[] roles, CancellationToken cancellationToken);
    Task<Result> Update(Guid id, string newName, string newEmail, string? newPassword, UserRoleType[] newRoles, CancellationToken cancellationToken);
    Task<Result> UpdateName(Guid id, string newName, CancellationToken cancellationToken);
    Task<Result> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<Result> Disable(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateEmailConfirmationToken(Guid id, CancellationToken cancellationToken);
    Task<Result<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken);
    Task<Result> VerifyPassword(Guid id, string currentPassword, CancellationToken cancellationToken);
    Task<Result> ConfirmChangeEmail(Guid id, string newEmail, string token, CancellationToken cancellationToken);
    Task<Result> VerifyConfirmChangePassword(Guid id, string token, CancellationToken cancellationToken);
    Task<Result> ConfirmChangePassword(Guid id, string newPassword, string token, CancellationToken cancellationToken);
}
