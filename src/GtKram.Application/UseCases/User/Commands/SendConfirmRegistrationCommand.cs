using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SendConfirmRegistrationCommand(Guid Id, string CallbackUrl) : ICommand<Result>;
