using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SignInOtpCommand(string Code, bool IsRememberClient) : ICommand<ErrorOr<Success>>;
