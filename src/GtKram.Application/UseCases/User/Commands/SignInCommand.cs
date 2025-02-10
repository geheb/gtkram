using GtKram.Domain.Base;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SignInCommand(string Email, string Password) : ICommand<Result<AuthResult>>;
