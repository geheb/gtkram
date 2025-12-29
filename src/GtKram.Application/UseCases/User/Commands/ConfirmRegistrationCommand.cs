using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ConfirmRegistrationCommand(Guid Id, string Password, string Token): ICommand<ErrorOr<Success>>;
