using GtKram.Infrastructure.Database.Models;
using System.Data.Common;

namespace GtKram.Infrastructure.Database.Repositories;

internal interface ISqlRepository<TEntity, TValue>
    where TEntity : JsonEntity<TValue>
    where TValue : new()
{
    string Table { get; }
    Task<IRepoTransaction> BeginTransaction(CancellationToken cancellationToken);
    Task<Guid> Insert(TEntity entity, CancellationToken cancellationToken);
    Task<object?> ExecuteScalar(TEntity entity, string sql, Dictionary<string, object?> values, CancellationToken cancellationToken);
    Task<object?> ExecuteScalar(string sql, Dictionary<string, object?> values, CancellationToken cancellationToken);
    Task<int> Delete(Guid id, CancellationToken cancellationToken);
    Task<TEntity?> SelectOne(Guid id, CancellationToken cancellationToken);
    Task<TEntity[]> SelectMany(ICollection<Guid> ids, CancellationToken cancellationToken);
    Task<TEntity[]> SelectAll(CancellationToken cancellationToken);
    Task<TEntity[]> SelectBy(int limit, string whereCondition, object? whereValues, CancellationToken cancellationToken);
    Task<bool> Update(TEntity entity, CancellationToken cancellationToken);
    Task<int> CountBy(string whereCondition, object? whereValues, CancellationToken cancellationToken);
}
