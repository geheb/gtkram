using GtKram.Application.UseCases.User.Models;

namespace GtKram.Application.Repositories;

public interface IUsers
{
    Task NotifyPasswordForgotten(string email, CancellationToken cancellationToken);
    Task<string[]?> Update(UserDto dto, string password, CancellationToken cancellationToken);
    Task<string[]?> Update(Guid id, string name);
    Task<string?> VerfiyChangePassword(Guid id, string token);
    Task<string?> VerifyConfirmRegistration(Guid id, string token);
    Task<(string[]? Error, string? Email)> ChangePassword(Guid id, string? token, string password);
    Task<(string? Error, bool IsFatal)> NotifyConfirmChangeEmail(Guid id, string newEmail, string currentPassword, CancellationToken cancellationToken);
    Task<string[]?> ConfirmRegistrationAndSetPassword(Guid id, string token, string password);
    Task<bool> NotifyConfirmRegistration(Guid id, CancellationToken cancellationToken);
    Task<UserDto[]> GetAll(CancellationToken cancellationToken);
    Task<UserDto?> Find(Guid id, CancellationToken cancellationToken);
    Task<bool> AddBillingRole(Guid userId, CancellationToken cancellationToken);
    Task<string[]?> Create(UserDto dto, CancellationToken cancellationToken);
    Task<string?> ConfirmChangeEmail(Guid id, string token, string encodedEmail);
    Task<Guid?> CreateSeller(string email, string name, CancellationToken cancellationToken);
}
