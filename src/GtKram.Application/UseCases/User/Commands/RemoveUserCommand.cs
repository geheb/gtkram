using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct RemoveUserCommand(Guid Id) : ICommand<ErrorOr<Success>>;
