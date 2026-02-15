using GtKram.Infrastructure.Database.Models;
using System.Data.Common;
using System.Linq.Expressions;

namespace GtKram.Infrastructure.Database.Repositories;

internal interface ISqlRepository<TEntity, TJsonValue> 
    where TEntity : class, IEntity, IEntityJsonValue<TJsonValue>
{
    DbTransaction? Transaction { set; }
    Task<DbTransaction> CreateTransaction(CancellationToken cancellationToken);
    Task Insert(TEntity entity, CancellationToken cancellationToken);
    Task<int> Delete(Guid id, CancellationToken cancellationToken);
    Task<TEntity?> SelectOne(Guid id, CancellationToken cancellationToken);
    Task<TEntity[]> SelectBy(
        int count,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken);
    Task<TEntity[]> SelectByJson(
        int count,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken);
    Task<TEntity[]> SelectMany(ICollection<Guid> ids, CancellationToken cancellationToken);
    Task<TEntity[]> SelectAll(CancellationToken cancellationToken);
    Task<bool> Update(TEntity entity, CancellationToken cancellationToken);
    Task<int> Update(IEnumerable<TEntity> entities, CancellationToken cancellationToken);
    Task<int> Count(CancellationToken cancellationToken);
    Task<int> CountBy(
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken);
    Task<int> MaxBy(
        Expression<Func<TEntity, object?>> maxField,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken);
}
