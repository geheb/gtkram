using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IBazaarSellerArticleRepository
{
    Task<Result> Create(BazaarSellerArticle model, CancellationToken cancellationToken);
    Task<Result> Create(BazaarSellerArticle[] models, Guid sellerId, CancellationToken cancellationToken);
    Task<BazaarSellerArticle[]> GetByBazaarSellerId(Guid id, CancellationToken cancellationToken);
    Task<BazaarSellerArticle[]> GetByBazaarSellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<Result<BazaarSellerArticle>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(BazaarSellerArticle model, CancellationToken cancellationToken);
}
