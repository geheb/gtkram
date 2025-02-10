using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct DisableOtpCommand(Guid Id, string Code) : ICommand<Result>;
