using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarEventRepository
{
    Task<Result<BazaarEvent>> Find(Guid id, CancellationToken cancellationToken);
}
