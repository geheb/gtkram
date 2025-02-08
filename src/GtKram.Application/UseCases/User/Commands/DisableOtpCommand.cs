using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record DisableOtpCommand(Guid Id, string Code) : ICommand<Result>;
