using GtKram.Infrastructure.Database.Entities;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace GtKram.Infrastructure.Repositories;

internal interface IRepository<T> where T : IEntity
{
    Task<DbTransaction> BeginTransaction(CancellationToken cancellationToken);

    Task Create(T entity, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task Create(T entity, CancellationToken cancellationToken) =>
        Create(entity, null, cancellationToken);

    Task<Entity<T>?> Find(Guid id, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<Entity<T>?> Find(Guid id, CancellationToken cancellationToken) =>
        Find(id, null, cancellationToken);

    Task<Entity<T>[]> Get(Guid[] ids, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<Entity<T>[]> Get(Guid[] ids, CancellationToken cancellationToken) =>
        Get(ids, null, cancellationToken);

    Task<Entity<T>[]> GetAll(IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<Entity<T>[]> GetAll(CancellationToken cancellationToken) =>
        GetAll(null, cancellationToken);

    Task<Entity<T>[]> Query(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<Entity<T>[]> Query(WhereFieldPair<T>[] where, CancellationToken cancellationToken) =>
        Query(where, null, cancellationToken);

    Task<int> Count(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<int> Count(WhereFieldPair<T>[] where, CancellationToken cancellationToken) =>
        Count(where, null, cancellationToken);

    Task<TResult?> Max<TResult>(Expression<Func<T, object?>> field, WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken) 
        where TResult : struct, IComparable;

    public Task<TResult?> Max<TResult>(Expression<Func<T, object?>> field, WhereFieldPair<T>[] where, CancellationToken cancellationToken)
        where TResult : struct, IComparable =>
        Max<TResult>(field, where, null, cancellationToken);

    Task<UpdateResult> Update(T entity, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<UpdateResult> Update(T entity, CancellationToken cancellationToken) =>
        Update(entity, null, cancellationToken);

    Task<UpdateResult> Update(T[] entities, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<UpdateResult> Update(T[] entities, CancellationToken cancellationToken) =>
        Update(entities, null, cancellationToken);

    Task<int> Delete(Guid id, IDbTransaction? trans, CancellationToken cancellationToken);

    public Task<int> Delete(Guid id, CancellationToken cancellationToken) =>
        Delete(id, null, cancellationToken);
}
