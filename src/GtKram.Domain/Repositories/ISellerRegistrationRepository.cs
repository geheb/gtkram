using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface ISellerRegistrationRepository
{
    Task<Result> Create(SellerRegistration model, CancellationToken cancellationToken);
    Task<Result<SellerRegistration>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result<SellerRegistration>> FindBySellerId(Guid id, CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetAll(CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken);
    Task<SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<Result> Update(SellerRegistration model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result<int>> GetCountByEventId(Guid id, CancellationToken cancellationToken);
    Task<Result<SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken);
}
