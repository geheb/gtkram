using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SendChangeEmailCommand(Guid Id, string NewEmail, string Password, string CallbackUrl) : ICommand<Result>;