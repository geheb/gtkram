using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarBillingArticleRepository
{
    Task<Result> Create(BazaarBillingArticle model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result> DeleteByBillingId(Guid id, CancellationToken cancellationToken);
    Task<BazaarBillingArticle[]> GetAll(CancellationToken cancellationToken);
    Task<BazaarBillingArticle[]> GetByBazaarBillingId(Guid id, CancellationToken cancellationToken);
}
