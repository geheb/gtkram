using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SignInOtpCommand(string Code, bool IsRememberClient) : ICommand<Result>;
