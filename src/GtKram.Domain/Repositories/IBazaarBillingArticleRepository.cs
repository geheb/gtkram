using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarBillingArticleRepository
{
    Task<Result<Guid>> Create(Guid billingId, Guid sellerArticleId, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result> DeleteByBillingId(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarBillingArticle>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result<BazaarBillingArticle>> FindBySellerArticleId(Guid sellerArticleId, CancellationToken cancellationToken);
    Task<BazaarBillingArticle[]> GetAll(CancellationToken cancellationToken);
    Task<BazaarBillingArticle[]> GetByBazaarBillingId(Guid id, CancellationToken cancellationToken);
    Task<BazaarBillingArticle[]> GetByBazaarBillingId(Guid[] ids, CancellationToken cancellationToken);
}
