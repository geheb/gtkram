using Dapper;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GtKram.Infrastructure.Repositories;

[DebuggerDisplay("Table = {_tableName}")]
internal sealed class Repository<T> : IRepository<T> where T : IEntity
{
    private static readonly string _tableName;
    private const string _selectById = "SELECT Id,Created,Modified,Version,Json FROM {0} WHERE Id=@Id";
    private const string _selectByIds = "SELECT Id,Created,Modified,Version,Json FROM {0} WHERE Id IN @Ids";
    private const string _select = "SELECT Id,Created,Modified,Version,Json FROM {0}";
    private const string _selectCount = "SELECT COUNT({1}) FROM {0}";
    private const string _selectMax = "SELECT MAX({1}) FROM {0}";
    private const string _insert = "INSERT INTO {0} (Id,Created,Version,Json) VALUES (@Id,@Created,@Version,@Json)";
    private const string _updateByIdAndVersion = "UPDATE {0} SET Json=@Json,Modified=@Modified,Version=@Version WHERE Id=@Id AND Version=@CurrentVersion";
    private const string _deleteById = "DELETE FROM {0} WHERE Id=@Id";

    private struct JsonEntity
    {
        public byte[]? Id { get; set; } 
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Modified { get; set; }
        public int Version { get; set; }
        public string? Json { get; set; }
    }

    private readonly TimeProvider _timeProvider;
    private readonly MySqlDbContext _dbContext;
    private readonly PkGenerator _pkGenerator = new();

    static Repository()
    {
        _tableName = typeof(T).GetCustomAttribute<TableAttribute>()!.Name;
    }

    public Repository(
        TimeProvider timeProvider,
        MySqlDbContext dbContext)
    {
        _timeProvider = timeProvider;
        _dbContext = dbContext;
    }

    public async Task<DbTransaction> BeginTransaction(CancellationToken cancellationToken)
    {
        var connection = await _dbContext.GetConnection(cancellationToken);
        return await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task Create(T entity, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = _pkGenerator.Generate();
        }

        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var sql = string.Format(_insert, _tableName);
        var parameters = new Dictionary<string, object>
        {
            { "@Id", entity.Id.ToBinary16() },
            { "@Created", entity.Created ?? _timeProvider.GetUtcNow() },
            { "@Version", 1 },
            { "@Json", JsonSerializer.Serialize(entity as object) }
        };

        await connection.ExecuteAsync(sql, parameters, trans);

        entity.Version = 1;
    }

    public async Task<Entity<T>?> Find(Guid id, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var sql = string.Format(_selectById, _tableName);
        var entity = await connection.QueryFirstOrDefaultAsync<JsonEntity>(sql, new { Id = id.ToBinary16() }, trans);

        return entity.Id is null ? null : Map(entity);
    }

    public async Task<Entity<T>[]> Get(Guid[] ids, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (ids.Length == 0) throw new ArgumentException(nameof(ids));

        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var sql = string.Format(_selectByIds, _tableName);
        var result = new List<Entity<T>>(ids.Length);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await connection.QueryAsync<JsonEntity>(sql, new { Ids = chunk.Select(c => c.ToBinary16()) }, trans);
            result.AddRange(Map(entities));
        }

        return [.. result];
    }

    public async Task<Entity<T>[]> GetAll(IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var sql = string.Format(_select, _tableName);

        return Map(await connection.QueryAsync<JsonEntity>(sql, null, trans));
    }

    public async Task<Entity<T>[]> Query(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (where.Length == 0) throw new ArgumentException(nameof(where));

        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = string.Format(_select, _tableName);

        var (query, parameters) = CreateQuery(sql, where);

        var result = await connection.QueryAsync<JsonEntity>(query, parameters, trans);
        if (!result?.Any() ?? false)
        {
            return [];
        }
        return Map(result!);
    }

    public async Task<int> Count(WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = string.Format(_selectCount, _tableName, "*");
        if (where.Length == 0)
        {
            return await connection.ExecuteScalarAsync<int>(sql, transaction: trans);
        }

        var (query, parameters) = CreateQuery(sql, where);

        return await connection.ExecuteScalarAsync<int>(query, parameters, trans);
    }

    public async Task<TResult?> Max<TResult>(Expression<Func<T, object?>> field, WhereFieldPair<T>[] where, IDbTransaction? trans, CancellationToken cancellationToken) 
        where TResult : struct, IComparable
    {
        if (where.Length == 0) throw new ArgumentException(nameof(where));

        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var column = $"_{field.GetPropertyName()}";

        var sql = string.Format(_selectMax, _tableName, column);
        var (query, parameters) = CreateQuery(sql, where);

        return await connection.ExecuteScalarAsync<TResult?>(query, parameters, trans);
    }

    public async Task<UpdateResult> Update(T entity, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = string.Format(_updateByIdAndVersion, _tableName, 0);
        var parameters = new Dictionary<string, object>
        {
            { "@Id", entity.Id.ToBinary16() },
            { "@Modified", _timeProvider.GetUtcNow() },
            { "@Version", entity.Version + 1 },
            { "@Json", JsonSerializer.Serialize(entity as object) },
            { "@CurrentVersion", entity.Version }
        };

        var affectedRows = await connection.ExecuteAsync(sql, parameters, trans);
        if (affectedRows == 0)
        {
            return UpdateResult.Conflict;
        }

        entity.Version++;

        return UpdateResult.Success;
    }

    public async Task<UpdateResult> Update(T[] entities, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        if (entities.Length == 0) throw new ArgumentException(nameof(entities));

        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = string.Format(_updateByIdAndVersion, _tableName);
        var parameters = new Dictionary<string, object>();

        foreach (var entity in entities)
        {
            parameters.Clear();
            parameters["@Id"] = entity.Id.ToBinary16();
            parameters["@Modified"] = _timeProvider.GetUtcNow();
            parameters["@Version"] = entity.Version + 1;
            parameters["@Json"] = JsonSerializer.Serialize(entity as object);
            parameters["@CurrentVersion"] = entity.Version;

            var affectedRows = await connection.ExecuteAsync(sql, parameters, trans);
            if (affectedRows == 0)
            {
                return UpdateResult.Conflict;
            }
        }

        Array.ForEach(entities, e => e.Version++);

        return UpdateResult.Success;
    }

    public async Task<int> Delete(Guid id, IDbTransaction? trans, CancellationToken cancellationToken)
    {
        var connection = trans?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var sql = string.Format(_deleteById, _tableName);

        return await connection.ExecuteAsync(sql, new { Id = id.ToBinary16() }, trans);
    }

    private static Entity<T> Map(JsonEntity entity)
    {
        var item = JsonSerializer.Deserialize<T>(entity.Json!)!;
        item.Version = entity.Version;
        return new Entity<T>(entity.Id!.FromBinary16(), entity.Created, entity.Modified, item);
    }

    private static Entity<T>[] Map(IEnumerable<JsonEntity> entities) =>
        entities.Any() ? [.. entities.Select(Map)] : [];

    private static (string Query, Dictionary<string, object> Parameters) CreateQuery(string query, WhereFieldPair<T>[] where)
    {
        var sql = new StringBuilder(query + " WHERE ");
        var parameters = new Dictionary<string, object>();
        var count = 0;
        foreach (var v in where)
        {
            if (count++ > 0)
            {
                sql.Append(" AND ");
            }

            if (v.Value is null)
            {
                sql.Append($"_{v.Field} IS NULL");
            }
            else
            {
                var param = "@P" + count;

                if (v.Value is Guid id)
                {
                    parameters[param] = id.ToChar32();
                }
                else if (v.Value is Guid[] ids)
                {
                    parameters[param] = ids.Select(id => id.ToChar32()).ToArray();
                }
                else
                {
                    parameters[param] = v.Value;
                }

                var isCollection = v.Value is not string && v.Value is System.Collections.IEnumerable;
                if (isCollection)
                {
                    sql.Append($"_{v.Field} IN {param}");
                }
                else
                {
                    sql.Append($"_{v.Field} = {param}");
                }
            }
        }

        return (sql.ToString(), parameters);
    }
}
