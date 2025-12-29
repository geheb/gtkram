using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IArticles
{
    Task<ErrorOr<Success>> Create(Article model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Create(Article[] models, Guid sellerId, CancellationToken cancellationToken);
    Task<Article[]> GetAll(CancellationToken cancellationToken);
    Task<Article[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Article[]> GetBySellerId(Guid id, CancellationToken cancellationToken);
    Task<Article[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<ErrorOr<int>> GetCountBySellerId(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Article>> Find(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Update(Article model, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Article>> FindBySellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken);
}
