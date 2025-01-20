using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.User.Handler;

internal sealed class UpdateUserHandler : ICommandHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IEmailValidatorService _emailValidatorService;

    public UpdateUserHandler(
        IUserRepository repository, 
        IEmailValidatorService emailValidatorService)
    {
        _repository = repository;
        _emailValidatorService = emailValidatorService;
    }

    public async ValueTask<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _repository.FindById(command.Id, cancellationToken);
        if (user is null)
        {
            return Result.Fail("Der Benutzer wurde nicht gefunden.");
        }
        if (!string.IsNullOrEmpty(command.Name))
        {
            var result = await _repository.UpdateName(command.Id, command.Name, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }
        if (!string.IsNullOrEmpty(command.Email))
        {
            if (!await _emailValidatorService.Validate(command.Email, cancellationToken))
            {
                return Result.Fail("Die E-Mail-Adresse ist ungültig.");
            }

            var result = await _repository.UpdateEmail(command.Id, command.Email, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }
        if (!string.IsNullOrEmpty(command.Password))
        {
            var result = await _repository.UpdatePassword(command.Id, command.Password, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }
        if (command.Roles is not null)
        {
            if (command.Roles.Length == 0)
            {
                return Result.Fail("Es wird mindestens eine Rolle benötigt.");
            }

            var result = await _repository.UpdateRoles(command.Id, command.Roles, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }
        return Result.Ok();
    }
}
