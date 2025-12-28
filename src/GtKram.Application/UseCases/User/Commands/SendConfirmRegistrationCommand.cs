using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SendConfirmRegistrationCommand(Guid Id, string CallbackUrl) : ICommand<ErrorOr<Success>>;
