using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.User.Handler;

internal sealed class UpsertUserHandler :
    ICommandHandler<CreateUserCommand, Result>,
    ICommandHandler<UpdateUserCommand, Result>,
    ICommandHandler<UpdateUsersNameCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IEmailValidatorService _emailValidatorService;

    public UpsertUserHandler(
        IUserRepository repository, 
        IEmailValidatorService emailValidatorService)
    {
        _repository = repository;
        _emailValidatorService = emailValidatorService;
    }

    public async ValueTask<Result> Handle(CreateUserCommand command, CancellationToken cancellationToken) =>
        await _repository.Create(command.Name, command.Email, command.Roles, cancellationToken);

    public async ValueTask<Result> Handle(UpdateUsersNameCommand command, CancellationToken cancellationToken) =>
        await _repository.UpdateName(command.Id, command.Name, cancellationToken);

    public async ValueTask<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _repository.UpdateName(command.Id, command.Name, cancellationToken);
        if (result.IsFailed)
        {
            return result;
        }
        
        if (!await _emailValidatorService.Validate(command.Email, cancellationToken))
        {
            return Result.Fail("Die E-Mail-Adresse ist ungültig.");
        }

        result = await _repository.UpdateEmail(command.Id, command.Email, cancellationToken);
        if (result.IsFailed)
        {
            return result;
        }
        
        if (!string.IsNullOrEmpty(command.Password))
        {
            result = await _repository.UpdatePassword(command.Id, command.Password, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }

        if (command.Roles.Length == 0)
        {
            return Result.Fail("Es wird mindestens eine Rolle benötigt.");
        }

        result = await _repository.UpdateRoles(command.Id, command.Roles, cancellationToken);
        if (result.IsFailed)
        {
            return result;
        }

        return Result.Ok();
    }
}
