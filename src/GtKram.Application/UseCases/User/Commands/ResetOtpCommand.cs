using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ResetOtpCommand(Guid Id) : ICommand<Result>;
