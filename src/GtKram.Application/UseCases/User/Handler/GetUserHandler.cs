namespace GtKram.Application.UseCases.User.Handler;

using FluentResults;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;
using System.Threading;
using System.Threading.Tasks;

internal sealed class GetUserHandler : 
    IQueryHandler<GetAllUsersQuery, Domain.Models.User[]>,
    IQueryHandler<FindUserByIdQuery, Result<Domain.Models.User>>
{
    private readonly IUserRepository _userRepository;

    public GetUserHandler(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async ValueTask<Domain.Models.User[]> Handle(GetAllUsersQuery query, CancellationToken cancellationToken) =>
        await _userRepository.GetAll(cancellationToken);

    public async ValueTask<Result<User>> Handle(FindUserByIdQuery query, CancellationToken cancellationToken) =>
        await _userRepository.FindById(query.Id, cancellationToken);
}
