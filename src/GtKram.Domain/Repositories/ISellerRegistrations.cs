using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ISellerRegistrations
{
    Task<ErrorOr<Success>> Create(SellerRegistration model, CancellationToken cancellationToken);
    Task<ErrorOr<SellerRegistration>> Find(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<SellerRegistration>> FindBySellerId(Guid id, CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetAll(CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(SellerRegistration model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<int>> GetCountByEventId(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken);
}
