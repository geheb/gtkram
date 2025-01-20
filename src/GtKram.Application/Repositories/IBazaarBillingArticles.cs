using GtKram.Application.UseCases.Bazaar.Models;

namespace GtKram.Application.Repositories;

public interface IBazaarBillingArticles
{
    Task<BazaarBillingArticleDto[]> GetAll(Guid eventId, Guid billingId, CancellationToken cancellationToken);
    Task<(BazaarArticleStatus status, Guid? billingArticleId)> Create(Guid eventId, Guid billingId, int sellerNumber, int labelNumber, CancellationToken cancellationToken);
    Task<(BazaarArticleStatus status, Guid? billingArticleId)> Create(Guid eventId, Guid billingId, Guid articleId, CancellationToken cancellationToken);
    Task<BazaarBillingArticleDto?> Find(Guid id, CancellationToken cancellationToken);
    Task<bool> Delete(Guid eventId, Guid billingId, Guid id, CancellationToken cancellationToken);
    Task<bool> Cancel(Guid eventId, Guid billingId, CancellationToken cancellationToken);
}
