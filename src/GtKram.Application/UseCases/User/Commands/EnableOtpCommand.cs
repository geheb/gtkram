using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record EnableOtpCommand(Guid Id, string Code) : ICommand<Result>;
