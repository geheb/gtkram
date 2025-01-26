using FluentResults;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SignInCommand(string Email, string Password) : ICommand<Result<AuthResult>>;
