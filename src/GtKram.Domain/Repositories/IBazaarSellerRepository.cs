using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerRepository
{
    Task<Result> Create(BazaarSeller model, CancellationToken cancellationToken);
    Task<Result<BazaarSeller>> Find(Guid id, CancellationToken cancellationToken);
    Task<BazaarSeller[]> GetByBazaarEventId(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSeller model, CancellationToken cancellationToken);
}
