using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SendConfirmRegistrationCommand(Guid Id, string CallbackUrl) : ICommand<Result>;
