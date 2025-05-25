using GtKram.Infrastructure.Persistence.Entities;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace GtKram.Infrastructure.Repositories;

internal interface IRepository<T> where T : IEntity
{
    Task<DbTransaction> BeginTransaction(CancellationToken cancellationToken);
    Task Create(T entity, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<Entity<T>?> Find(Guid id, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<Entity<T>[]> Get(Guid[] ids, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<Entity<T>[]> Get(IDbTransaction? trans, CancellationToken cancellationToken);
    Task<Entity<T>[]> Query(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<int> Count(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<TResult?> Max<TResult>(Expression<Func<T, object?>> field, WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken) 
        where TResult : struct, IComparable;
    Task<UpdateResult> Update(T entity, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<UpdateResult> Update(T[] entities, IDbTransaction? trans, CancellationToken cancellationToken);
    Task<int> Delete(Guid id, IDbTransaction? trans, CancellationToken cancellationToken);
}
