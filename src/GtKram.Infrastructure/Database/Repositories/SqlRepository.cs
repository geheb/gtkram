using Dapper;
using GtKram.Infrastructure.Database.Models;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Repositories;

internal sealed class SqlRepository<TEntity, TValue> : ISqlRepository<TEntity, TValue>
    where TEntity : JsonEntity<TValue>
    where TValue : new()
{
    private static readonly string _table;

    private const string _setVersionUpdatedDataFields = $"""
        "{nameof(JsonEntity<>.Version)}" = "{nameof(JsonEntity<>.Version)}" + 1,
        "{nameof(JsonEntity<>.Updated)}" = @_{nameof(JsonEntity<>.Updated)},
        "{nameof(JsonEntity<>.Data)}" = @_{ nameof(JsonEntity<>.Data)}
        """;

    private const string _setVersionUpdatedFields = $"""
        "{nameof(JsonEntity<>.Version)}" = "{nameof(JsonEntity<>.Version)}" + 1,
        "{nameof(JsonEntity<>.Updated)}" = @_{nameof(JsonEntity<>.Updated)}
        """;

    private const string _insertFields = $"""
        "{nameof(JsonEntity<>.Id)}",
        "{nameof(JsonEntity<>.Created)}",
        "{nameof(JsonEntity<>.Version)}",
        "{nameof(JsonEntity<>.Data)}"
        """;

    private const string _insertParams = $"""
        @_{nameof(JsonEntity<>.Id)},
        @_{nameof(JsonEntity<>.Created)},
        @_{nameof(JsonEntity<>.Version)},
        @_{nameof(JsonEntity<>.Data)}
        """;

    private const string _insertParamsWithoutData = $"""
        @_{nameof(JsonEntity<>.Id)},
        @_{nameof(JsonEntity<>.Created)},
        @_{nameof(JsonEntity<>.Version)}
        """;

    private readonly TimeProvider _timeProvider;
    private readonly PostgresDbContext _dbContext;
    internal DbTransaction? _transaction;

    public string Table => _table;

    static SqlRepository()
    {
        var attribute = typeof(TEntity).GetCustomAttribute<JsonTableAttribute>()!;
        _table = $"\"{attribute.Schema}\".\"{attribute.Name}\"";
    }

    public SqlRepository(
        TimeProvider timeProvider,
        PostgresDbContext dbContext)
    {
        _timeProvider = timeProvider;
        _dbContext = dbContext;
    }

    public async Task<IRepoTransaction> BeginTransaction(CancellationToken cancellationToken)
    {
        _transaction = await _dbContext.BeginTransaction(cancellationToken);
        return new RepoTransaction(this);
    }

    public async Task<Guid> Insert(TEntity entity, CancellationToken cancellationToken)
    {
        using var data = JsonSerializer.SerializeToDocument(entity.Value);

        var id = entity.Id == default ? Guid.CreateVersion7() : entity.Id;

        var values = new Dictionary<string, object?>
        {
            [$"@_{nameof(JsonEntity<>.Id)}"] = id,
            [$"@_{nameof(JsonEntity<>.Created)}"] = entity.Created == default ? _timeProvider.GetUtcNow() : entity.Created,
            [$"@_{nameof(JsonEntity<>.Version)}"] = 1,
            [$"@_{nameof(JsonEntity<>.Data)}"] = data
        };

        var query = BuildInsertTableQuery();

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        await connection.ExecuteAsync(query, values, _transaction);

        return id;
    }

    public async Task<object?> ExecuteScalar(
        TEntity entity, 
        string sql, 
        Dictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        using var data = JsonSerializer.SerializeToDocument(entity.Value);

        var query = BuildQuery(sql);

        values[$"@_{nameof(JsonEntity<>.Id)}"] = entity.Id == default ? Guid.CreateVersion7() : entity.Id;
        if (entity.Created == default)
        {
            values[$"@_{nameof(JsonEntity<>.Created)}"] = _timeProvider.GetUtcNow();
        }
        else
        {
            values[$"@_{nameof(JsonEntity<>.Updated)}"] = _timeProvider.GetUtcNow();
        }
        values[$"@_{nameof(JsonEntity<>.Version)}"] = entity.Version == default ? 1 : entity.Version;
        values[$"@_{nameof(JsonEntity<>.Data)}"] = data;

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var result = await connection.ExecuteScalarAsync(query, values, _transaction);

        return result;
    }

    public async Task<object?> ExecuteScalar(
        string sql,
        Dictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        values[$"@_{nameof(JsonEntity<>.Updated)}"] = _timeProvider.GetUtcNow();

        var query = BuildQuery(sql);

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var result = await connection.ExecuteScalarAsync(query, values, _transaction);

        return result;
    }

    public async Task<int> Delete(Guid id, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var query = $"""DELETE FROM {_table} WHERE "{nameof(JsonEntity<>.Id)}"=@id""";

        return await connection.ExecuteAsync(query, new { id }, _transaction);
    }

    public async Task<TEntity?> SelectOne(Guid id, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var query = BuildSelectTableQuery(0, $""" "{nameof(JsonEntity<>.Id)}"=@id """);
        var entity = await connection.QueryFirstOrDefaultAsync<TEntity>(query, new { id }, _transaction);

        return entity is null ? null : Deserialize(entity);
    }

    public async Task<TEntity[]> SelectMany(ICollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) throw new ArgumentException(nameof(ids));

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var result = new List<TEntity>(ids.Count);

        var query = BuildSelectTableQuery(0, $""" ("{nameof(JsonEntity<>.Id)}")::uuid = ANY(@ids) """);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await connection.QueryAsync<TEntity>(query, new { ids = chunk }, _transaction);
            result.AddRange(entities.Select(Deserialize));
        }

        return [.. result];
    }

    public async Task<TEntity[]> SelectAll(CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var query = BuildSelectTableQuery(0, null);
        var entities = await connection.QueryAsync<TEntity>(query, _transaction);

        return [.. entities.Select(Deserialize)];
    }

    public async Task<TEntity[]> SelectBy(
        int limit,
        string whereCondition,
        object? whereValues,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = BuildSelectTableQuery(limit, BuildQuery(whereCondition));

        var entities = await connection.QueryAsync<TEntity>(sql, whereValues, _transaction);
        return [.. entities.Select(Deserialize)];
    }

    public async Task<bool> Update(TEntity entity, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        using var data = JsonSerializer.SerializeToDocument(entity.Value);

        var values = new Dictionary<string, object?>
        {
            [$"@_{nameof(JsonEntity<>.Id)}"] = entity.Id,
            [$"@_{nameof(JsonEntity<>.Updated)}"] = _timeProvider.GetUtcNow(),
            [$"@_{nameof(JsonEntity<>.Version)}"] = entity.Version,
            [$"@_{nameof(JsonEntity<>.Data)}"] = data
        };

        var query = BuildUpdateTableQuery();

        var affectedRows = await connection.ExecuteAsync(query, values, _transaction);
        if (affectedRows == 0)
        {
            return false;
        }

        return true;
    }

    public async Task<int> CountBy(
        string whereCondition,
        object? whereValues,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        var sql = BuildCountTableQuery(BuildQuery(whereCondition));

        return await connection.ExecuteScalarAsync<int?>(sql, whereValues, _transaction) ?? default;
    }

    private static string BuildQuery(string sql) =>
        sql
        .Replace("$table", _table)
        .Replace("$id_field", $"\"{nameof(JsonEntity<>.Id)}\"")
        .Replace("$id_param", $"@_{nameof(JsonEntity<>.Id)}")
        .Replace("$insert_fields", _insertFields)
        .Replace("$insert_table_query", BuildInsertTableQuery())
        .Replace("$update_table_query", BuildUpdateTableQuery())
        .Replace("$created_field", $"\"{nameof(JsonEntity<>.Created)}\"")
        .Replace("$created_param", $"@_{nameof(JsonEntity<>.Created)}")
        .Replace("$version_field", $"\"{nameof(JsonEntity<>.Version)}\"")
        .Replace("$updated_field", $"\"{nameof(JsonEntity<>.Updated)}\"")
        .Replace("$updated_param", $"@_{nameof(JsonEntity<>.Updated)}")
        .Replace("$data_field", $"\"{nameof(JsonEntity<>.Data)}\"")
        .Replace("$data_param", $"@_{nameof(JsonEntity<>.Data)}")
        .Replace("$insert_params_without_data", _insertParamsWithoutData)
        .Replace("$insert_params", _insertParams)
        .Replace("$set_version_updated", _setVersionUpdatedFields);

    private static TEntity Deserialize(TEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Data))
        {
            entity.Value = JsonSerializer.Deserialize<TValue>(entity.Data)!;
            entity.Data = null;
        }
        return entity;
    }

    private static string BuildInsertTableQuery() =>
        $"""
        INSERT INTO {_table} (
            {_insertFields}
        ) 
        VALUES (
            {_insertParams}
        );
        """;

    private static string BuildUpdateTableQuery()
    {
        const string where = $""" 
            "{nameof(JsonEntity<>.Id)}" = @_{nameof(JsonEntity<>.Id)} AND "{nameof(JsonEntity<>.Version)}" = @_{nameof(JsonEntity<>.Version)}
            """;

        return $"UPDATE {_table} SET {_setVersionUpdatedDataFields} WHERE {where};";
    }

    private static string BuildSelectTableQuery(int limit, string? where)
    {
        const string columns = $"""
            "{nameof(JsonEntity<>.Id)}",
            "{nameof(JsonEntity<>.Created)}",
            "{nameof(JsonEntity<>.Updated)}",
            "{nameof(JsonEntity<>.Version)}",
            "{nameof(JsonEntity<>.Data)}"
            """;

        if (where is null)
        {
            return limit > 0
                ? $"SELECT {columns} FROM {_table} LIMIT {limit};"
                : $"SELECT {columns} FROM {_table};";
        }

        return limit > 0
            ? $"SELECT {columns} FROM {_table} WHERE {where} LIMIT {limit};"
            : $"SELECT {columns} FROM {_table} WHERE {where};";
    }

    private static string BuildCountTableQuery(string? where)
    {
        if (where is null)
        {
            return $"SELECT COUNT(*) FROM {_table};";
        }

        return $"SELECT COUNT(*) FROM {_table} WHERE {where};";
    }

    private readonly struct RepoTransaction : IRepoTransaction
    {
        private readonly SqlRepository<TEntity, TValue> _repository;

        public RepoTransaction(SqlRepository<TEntity, TValue> repository) =>
            _repository = repository;

        public Task Commit(CancellationToken cancellationToken) =>
            _repository._transaction!.CommitAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_repository._transaction is null)
            {
                return;
            }
            var transaction = _repository._transaction!;
            _repository._transaction = null;
            await transaction.DisposeAsync();
        }
    }
}