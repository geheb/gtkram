using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Domain.Repositories;

public interface IArticles
{
    Task<Result> Create(Article model, CancellationToken cancellationToken);
    Task<Result> Create(Article[] models, Guid sellerId, CancellationToken cancellationToken);
    Task<Article[]> GetAll(CancellationToken cancellationToken);
    Task<Article[]> GetById(Guid[] ids, CancellationToken cancellationToken);
    Task<Article[]> GetBySellerId(Guid id, CancellationToken cancellationToken);
    Task<Article[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken);
    Task<Result<int>> GetCountBySellerId(Guid id, CancellationToken cancellationToken);
    Task<Result<Article>> Find(Guid id, CancellationToken cancellationToken);
    Task<Result> Update(Article model, CancellationToken cancellationToken);
    Task<Result> Delete(Guid id, CancellationToken cancellationToken);
    Task<Result<Article>> FindBySellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken);
}
