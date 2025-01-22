using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Models;
using GtKram.Application.UseCases.User.Queries;
using Mediator;

namespace GtKram.Application.UseCases.User.Handler;

internal sealed class AuthHandler :
    ICommandHandler<EnableTwoFactorAuthCommand, Result>,
    ICommandHandler<DisableTwoFactorAuthCommand, Result>,
    ICommandHandler<ResetTwoFactorAuthCommand, Result>,
    ICommandHandler<CreateTwoFactorAuthCommand, Result<UserTwoFactorAuthSettings>>,
    IQueryHandler<GetTwoFactorAuthQuery, Result<UserTwoFactorAuthSettings>>
{
    private readonly ITwoFactorAuth _twoFactorAuth;

    public AuthHandler(ITwoFactorAuth twoFactorAuth)
    {
        _twoFactorAuth = twoFactorAuth;
    }

    public async ValueTask<Result> Handle(EnableTwoFactorAuthCommand command, CancellationToken cancellationToken) =>
        await _twoFactorAuth.Enable(command.Id, true, command.Code);

    public async ValueTask<Result> Handle(DisableTwoFactorAuthCommand command, CancellationToken cancellationToken) =>
        await _twoFactorAuth.Enable(command.Id, false, command.Code);

    public async ValueTask<Result> Handle(ResetTwoFactorAuthCommand command, CancellationToken cancellationToken) =>
        await _twoFactorAuth.Reset(command.Id);

    public async ValueTask<Result<UserTwoFactorAuthSettings>> Handle(CreateTwoFactorAuthCommand command, CancellationToken cancellationToken) =>
        await _twoFactorAuth.CreateAuthenticator(command.Id);

    public async ValueTask<Result<UserTwoFactorAuthSettings>> Handle(GetTwoFactorAuthQuery command, CancellationToken cancellationToken) =>
        await _twoFactorAuth.GetAuthenticator(command.Id);
}