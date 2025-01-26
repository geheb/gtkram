using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ResetOtpCommand(Guid Id) : ICommand<Result>;
