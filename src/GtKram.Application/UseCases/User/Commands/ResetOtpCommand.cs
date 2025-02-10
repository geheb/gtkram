using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ResetOtpCommand(Guid Id) : ICommand<Result>;
