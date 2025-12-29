using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ResetOtpCommand(Guid Id) : ICommand<ErrorOr<Success>>;
