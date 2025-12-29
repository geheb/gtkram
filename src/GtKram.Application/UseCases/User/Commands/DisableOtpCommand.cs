using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct DisableOtpCommand(Guid Id, string Code) : ICommand<ErrorOr<Success>>;
