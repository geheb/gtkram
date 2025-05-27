using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GtKram.Application.Tests;

internal sealed class RepositoryMock<T> : IRepository<T> where T : IEntity
{
    private static readonly Dictionary<string, List<EntityItem>> _entities = new();
    private static readonly string _tableName;

    private readonly string _prefix;

    private sealed class EntityItem
    {
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public object Item { get; set; } = null!;

        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (obj is null || obj is not EntityItem item)
            {
                return false;
            }
            return item.Id == Id;
        }
    }

    private sealed class EmptyDbTransaction : DbTransaction
    {
        public override System.Data.IsolationLevel IsolationLevel { get; }

        protected override DbConnection? DbConnection { get; }

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }

    static RepositoryMock()
    {
        _tableName = typeof(T).GetCustomAttribute<TableAttribute>()!.Name;
    }

    public RepositoryMock(string prefix) => _prefix = prefix;

    public Task<int> Count(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult(0);
        }

        var count = 0;
        Match(entities, where, e => count++);

        return Task.FromResult(count);
    }

    public Task Create(T entity, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var item = new EntityItem
        {
            Id = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            Item = entity
        };

        entity.Id = item.Id;
        entity.Version = 1;

        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            entities = new();
            _entities.Add(_prefix + _tableName, entities);
        }
        entities.Add(item);
        return Task.CompletedTask;
    }

    public Task<int> Delete(Guid id, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult(0);
        }
        var item = entities.Find(e => e.Id == id);
        if (item is null)
        {
            return Task.FromResult(0);
        }
        entities.Remove(item);
        return Task.FromResult(1);
    }

    public Task<Entity<T>?> Find(Guid id, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult<Entity<T>?>(default);
        }
        var item = entities.Find(e => e.Id == id);
        return item is null
            ? Task.FromResult<Entity<T>?>(default)
            : Task.FromResult<Entity<T>?>(new Entity<T>(item.Id, item.Created, item.Modified, (T)item.Item));
    }

    public Task<Entity<T>[]> Get(Guid[] ids, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult<Entity<T>[]>([]);
        }
        var items = entities.FindAll(e => ids.Contains(e.Id));
        return Task.FromResult(items.Select(item => new Entity<T>(item.Id, item.Created, item.Modified, (T)item.Item)).ToArray());
    }

    public Task<Entity<T>[]> GetAll(IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult<Entity<T>[]>([]);
        }
        return Task.FromResult(entities.Select(item => new Entity<T>(item.Id, item.Created, item.Modified, (T)item.Item)).ToArray());
    }

    public Task<Entity<T>[]> Query(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult<Entity<T>[]>([]);
        }

        var result = new List<Entity<T>>();

        Match(entities, where, e => result.Add(new Entity<T>(e.Id, e.Created, e.Modified, (T)e.Item)));

        return Task.FromResult(result.ToArray());
    }

    public Task<UpdateResult> Update(T entity, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult(UpdateResult.Conflict);
        }

        var item = entities.Find(e => e.Id == entity.Id);
        if (item is null || ((T)item.Item).Version != entity.Version)
        {
            return Task.FromResult(UpdateResult.Conflict);
        }

        entity.Version++;
        item.Modified = DateTimeOffset.UtcNow;
        item.Item = entity;
        return Task.FromResult(UpdateResult.Success);
    }

    public Task<DbTransaction> BeginTransaction(CancellationToken cancellationToken) =>
        Task.FromResult<DbTransaction>(new EmptyDbTransaction());

    public Task<TResult?> Max<TResult>(Expression<Func<T, object?>> field, WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken) 
        where TResult : struct, IComparable
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entities))
        {
            return Task.FromResult<TResult?>(default);
        }

        var items = new List<object>();

        Match(entities, where, e => items.Add(e.Item));

        if (items.Count == 0)
        {
            return Task.FromResult<TResult?>(default);
        }

        Dictionary<string, object?> props = new();
        TResult? lastValue = default;

        var comparer = Comparer<TResult>.Default;
        var column = field.GetPropertyName();

        foreach (var item in items)
        {
            GetProperties(item, props);

            var value = (TResult?)props[column];
            if (lastValue is null && value is not null)
            {
                lastValue = value;
            }
            else if (value is not null && comparer.Compare(value.Value, lastValue!.Value) > 0)
            {
                lastValue = value;
            }
        }

        return Task.FromResult(lastValue);
    }

    public Task<UpdateResult> Update(T[] entities, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (!_entities.TryGetValue(_prefix + _tableName, out var entityItems))
        {
            return Task.FromResult(UpdateResult.Conflict);
        }

        foreach (var entity in entities)
        {
            var item = entityItems.Find(e => e.Id == entity.Id);
            if (item is null || ((T)item.Item).Version != entity.Version)
            {
                return Task.FromResult(UpdateResult.Conflict);
            }

            entity.Version++;
            item.Modified = DateTimeOffset.UtcNow;
            item.Item = entity;
        }

        return Task.FromResult(UpdateResult.Success);
    }

    private static void Match(List<EntityItem> entities, WhereFieldPair<T>[] where, Action<EntityItem> entityMatch)
    {
        var count = 0;
        Dictionary<string, object?> props = new();

        foreach (var entity in entities)
        {
            count = 0;

            GetProperties(entity.Item, props);

            foreach (var v in where)
            {
                if (v.IsCollection)
                {
                    foreach (var arrayValue in (IEnumerable)v.Value!)
                    {
                        var value = arrayValue;
                        if (arrayValue is byte[] byteArray)
                        {
                            value = new Guid(byteArray);
                        }
                        if (object.Equals(props[v.Field], value))
                        {
                            count++;
                            break;
                        }
                    }
                }
                else
                {
                    var value = v.Value; ;
                    if (value is byte[] byteArray)
                    {
                        value = new Guid(byteArray);
                    }
                    if (object.Equals(props[v.Field], value))
                    {
                        count++;
                    }
                }
            }

            if (count == where.Length)
            {
                entityMatch.Invoke(entity);
            }
        }
    }

    private static void GetProperties(object item, Dictionary<string, object?> props)
    {
        props.Clear();
        foreach (var prop in item.GetType().GetProperties().Where(x => x.CanRead && x.CanWrite))
        {
            props[prop.Name] = prop.GetValue(item);
        }
    }
}
