using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Models;

namespace GtKram.Application.Repositories;

public interface IBazaarSellers
{
    Task<BazaarSellerDto[]> GetAll(Guid userId, CancellationToken cancellationToken);
    Task<BazaarSellerDto?> Find(Guid sellerId, CancellationToken cancellationToken);
    Task<BazaarSellerDto?> Find(Guid eventId, Guid userId, CancellationToken cancellationToken);
    Task<bool> Update(Guid id, SellerRole role, int sellerNumber, bool canCreateBillings, CancellationToken cancellationToken);
}
