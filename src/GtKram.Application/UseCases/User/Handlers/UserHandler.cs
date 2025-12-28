using ErrorOr;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class UserHandler :
    IQueryHandler<GetAllUsersQuery, Domain.Models.User[]>,
    IQueryHandler<FindUserByIdQuery, ErrorOr<Domain.Models.User>>,
    ICommandHandler<CreateUserCommand, ErrorOr<Guid>>,
    ICommandHandler<UpdateUserCommand, ErrorOr<Success>>
{
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IMediator _mediator;
    private readonly IUsers _users;
    private readonly IEmailValidatorService _emailValidatorService;

    public UserHandler(
        IdentityErrorDescriber errorDescriber,
        IMediator mediator,
        IUsers users, 
        IEmailValidatorService emailValidatorService)
    {
        _errorDescriber = errorDescriber;
        _mediator = mediator;
        _users = users;
        _emailValidatorService = emailValidatorService;
    }

    public async ValueTask<Domain.Models.User[]> Handle(GetAllUsersQuery query, CancellationToken cancellationToken) =>
        await _users.GetAll(cancellationToken);

    public async ValueTask<ErrorOr<Domain.Models.User>> Handle(FindUserByIdQuery query, CancellationToken cancellationToken)
    {
        var result = await _users.FindById(query.Id, cancellationToken);
        return result;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (!await _emailValidatorService.Validate(command.Email, cancellationToken))
        {
            return _errorDescriber.InvalidEmail(command.Email).ToError();
        }
        
        var idResult = await _users.Create(command.Name, command.Email, command.Roles, cancellationToken);
        if (idResult.IsError)
        {
            return idResult;
        }

        var result = await _mediator.Send(new SendConfirmRegistrationCommand(idResult.Value, command.CallbackUrl), cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return idResult;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _users.Update(command.Id, command.Name, command.Roles, cancellationToken);
        return result;
    }
}
