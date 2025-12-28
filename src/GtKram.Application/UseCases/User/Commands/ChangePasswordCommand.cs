using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ChangePasswordCommand(Guid Id, string CurrentPassword, string NewPassword) : ICommand<ErrorOr<Success>>;