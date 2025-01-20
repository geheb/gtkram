using GtKram.Application.UseCases.Bazaar.Models;

namespace GtKram.Application.Repositories;

public interface IBazaarSellerArticles
{
    Task<BazaarSellerArticleDto[]> GetAll(Guid bazaarSellerId, Guid userId, CancellationToken cancellationToken);
    Task<bool> Create(Guid bazaarSellerId, Guid userId, BazaarSellerArticleDto article, CancellationToken cancellationToken);
    Task<BazaarSellerArticleDto?> Find(Guid sellerId, int labelNumber, CancellationToken cancellationToken);
    Task<BazaarSellerArticleDto?> Find(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<bool> Delete(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<bool> TakeOverArticles(Guid bazaarSellerId, Guid userId, CancellationToken cancellationToken);
    Task<bool> Update(Guid bazaarSellerId, Guid userId, BazaarSellerArticleDto article, CancellationToken cancellationToken);
}
