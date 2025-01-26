using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class UserHandler :
    IQueryHandler<GetAllUsersQuery, Domain.Models.User[]>,
    IQueryHandler<FindUserByIdQuery, Result<Domain.Models.User>>,
    ICommandHandler<CreateUserCommand, Result<Guid>>,
    ICommandHandler<UpdateUserCommand, Result>
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _repository;
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly IEmailValidatorService _emailValidatorService;

    public UserHandler(
        IMediator mediator,
        IUserRepository repository, 
        IUserAuthenticator userAuthenticator,
        IEmailValidatorService emailValidatorService)
    {
        _mediator = mediator;
        _repository = repository;
        _userAuthenticator = userAuthenticator;
        _emailValidatorService = emailValidatorService;
    }

    public async ValueTask<Domain.Models.User[]> Handle(GetAllUsersQuery query, CancellationToken cancellationToken) =>
        await _repository.GetAll(cancellationToken);

    public async ValueTask<Result<Domain.Models.User>> Handle(FindUserByIdQuery query, CancellationToken cancellationToken) =>
        await _repository.Find(query.Id, cancellationToken);

    public async ValueTask<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (!await _emailValidatorService.Validate(command.Email, cancellationToken))
        {
            return Result.Fail("Die E-Mail-Adresse ist ungültig.");
        }
        
        var idResult = await _repository.Create(command.Name, command.Email, command.Roles, cancellationToken);
        if (idResult.IsFailed)
        {
            return idResult;
        }

        var result = await _mediator.Send(new SendConfirmRegistrationCommand(idResult.Value, command.CallbackUrl), cancellationToken);
        if (result.IsFailed)
        {
            return result;
        }

        return idResult;
    }

    public async ValueTask<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken) =>
        await _repository.Update(command.Id, command.Name, command.Roles, cancellationToken);
}
