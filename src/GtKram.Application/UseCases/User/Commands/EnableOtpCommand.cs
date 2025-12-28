using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct EnableOtpCommand(Guid Id, string Code) : ICommand<ErrorOr<Success>>;
