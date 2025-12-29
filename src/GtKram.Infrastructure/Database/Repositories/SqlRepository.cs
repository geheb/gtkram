using Dapper;
using GtKram.Infrastructure.Database.Models;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GtKram.Infrastructure.Database.Repositories;

internal sealed class SqlRepository<TEntity> : ISqlRepository<TEntity> where TEntity : class, IEntity
{
    private static readonly string _tableName;
    private static string _selectColumnNames;
    private static readonly string _insertOne;
    private static readonly string _deleteOne;
    private static readonly string _selectOne;
    private static readonly string _selectMany;
    private static readonly string _selectAll;
    private static readonly string _updateOne;
    private static readonly string _countAll;
    private readonly TimeProvider _timeProvider;
    private readonly SQLiteDbContext _dbContext;
    private DbTransaction? _transaction;

    public DbTransaction? Transaction
    {
        set {  _transaction = value; }
    }

    static SqlRepository()
    {
        var attribute = typeof(TEntity).GetCustomAttribute<JsonTableAttribute>();
        _tableName = attribute!.Name;

        _selectColumnNames = string.Join(',',
            [
                nameof(IEntity.Id),
                nameof(IEntity.Created),
                nameof(IEntity.Updated),
                nameof(IEntity.JsonProperties),
                nameof(IEntity.JsonVersion),
            ]);

        string[] names =
        [
            nameof(IEntity.Id),
            nameof(IEntity.Created),
            nameof(IEntity.JsonProperties),
            nameof(IEntity.JsonVersion),
            .. attribute.MapColumns ?? []
        ];

        var insertColumnNames = string.Join(',', names);
        var insertValues = "@" + string.Join(",@", names);

        var update =
            BuildWhere(nameof(IEntity.Updated), DateTime.MinValue) + "," +
            BuildWhere(nameof(IEntity.JsonProperties), string.Empty) + "," +
            $"{nameof(IEntity.JsonVersion)}={nameof(IEntity.JsonVersion)}+1";

        foreach (var n in attribute.MapColumns ?? [])
        {
            update += $",{n}=@{n}";
        }

        var whereId = BuildWhere(nameof(IEntity.Id), Guid.Empty);
        var whereJsonVersion = BuildWhere(nameof(IEntity.JsonVersion), 0);
        _insertOne = $"INSERT INTO {_tableName} ({insertColumnNames}) VALUES ({insertValues})";
        _deleteOne = $"DELETE FROM {_tableName} WHERE {whereId}";
        _selectOne = BuildSelect(0, nameof(IEntity.Id), Guid.Empty);
        _selectMany = BuildSelect(0, nameof(IEntity.Id), Array.Empty<Guid>());
        _selectAll = BuildSelect(0, null, null);
        _updateOne = $"UPDATE {_tableName} SET {update} WHERE {whereId} AND {whereJsonVersion}";
        _countAll = BuildCount(null, null);
    }

    public SqlRepository(
        TimeProvider timeProvider,
        SQLiteDbContext dbContext)
    {
        _timeProvider = timeProvider;
        _dbContext = dbContext;
    }

    public async Task<DbTransaction> CreateTransaction(CancellationToken cancellationToken) =>
        _transaction = await _dbContext.BeginTransaction(cancellationToken);

    public async Task Insert(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.CreateVersion7();
        }

        if (entity.Created == DateTime.MinValue)
        {
            entity.Created = _timeProvider.GetUtcNow().DateTime;
        }

        entity.JsonVersion = 1;
        entity.Serialize();

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        await connection.ExecuteAsync(_insertOne, entity, _transaction);
    }

    public async Task<int> Delete(Guid id, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        return await connection.ExecuteAsync(_deleteOne, new { Id = id }, _transaction);
    }

    public async Task<TEntity?> SelectOne(Guid id, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var entity = await connection.QueryFirstOrDefaultAsync<TEntity>(_selectOne, new { Id = id }, _transaction);
        entity?.Deserialize();
        return entity;
    }

    public async Task<TEntity[]> SelectBy(
        int count,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var fieldName = whereField.GetPropertyName();

        var sql = BuildSelect(count, fieldName, whereValue);

        var parameters = whereValue is null ? null : new Dictionary<string, object?>
        {
            { $"@{fieldName}", whereValue },
        };

        var entities = await connection.QueryAsync<TEntity>(sql, parameters, _transaction);
        return entities.Select(e =>
        {
            e.Deserialize();
            return e;
        }).ToArray();
    }

    public async Task<TEntity[]> SelectByJson(
        int count,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var fieldName = whereField.GetPropertyName();

        var sql = BuildSelectJson(count, fieldName, whereValue);

        var parameters = whereValue is null ? null : new Dictionary<string, object?>
        {
            { $"@{fieldName}", whereValue },
        };
        var entities = await connection.QueryAsync<TEntity>(sql, parameters, _transaction);
        return entities.Select(e =>
        {
            e.Deserialize();
            return e;
        }).ToArray();
    }

    public async Task<TEntity[]> SelectMany(Guid[] ids, CancellationToken cancellationToken)
    {
        if (ids.Length == 0) throw new ArgumentException(nameof(ids));

        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var result = new List<TEntity>(ids.Length);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await connection.QueryAsync<TEntity>(_selectMany, new { Id = chunk }, _transaction);
            result.AddRange(entities.Select(e =>
                {
                    e.Deserialize();
                    return e;
                }));
        }

        return result.ToArray();
    }

    public async Task<TEntity[]> SelectAll(CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var entities = await connection.QueryAsync<TEntity>(_selectAll, _transaction);

        return entities.Select(e =>
        {
            e.Deserialize();
            return e;
        }).ToArray();
    }

    public async Task<bool> Update(TEntity entity, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);

        entity.Serialize();
        entity.Updated = _timeProvider.GetUtcNow().DateTime;

        var affectedRows = await connection.ExecuteAsync(_updateOne, entity, _transaction);
        if (affectedRows == 0)
        {
            return false;
        }

        entity.JsonVersion++;

        return true;
    }

    public async Task<int> Update(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var count = 0;

        foreach (var entity in entities)
        {
            entity.Serialize();
            entity.Updated = _timeProvider.GetUtcNow().DateTime;

            var affectedRows = await connection.ExecuteAsync(_updateOne, entity, _transaction);
            if (affectedRows == 0)
            {
                continue;
            }

            count++;
            entity.JsonVersion++;
        }

        return count;
    }

    public async Task<int> Count(CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        return await connection.ExecuteScalarAsync<int?>(_countAll, null, _transaction) ?? default;
    }

    public async Task<int> CountBy(
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var fieldName = whereField.GetPropertyName();

        var sql = BuildCount(fieldName, whereValue);

        var parameters = whereValue is null ? null : new Dictionary<string, object?>
        {
            { $"@{fieldName}", whereValue },
        };

        return await connection.ExecuteScalarAsync<int?>(sql, parameters, _transaction) ?? default;
    }

    public async Task<int> MaxBy(
        Expression<Func<TEntity, object?>> maxField,
        Expression<Func<TEntity, object?>> whereField,
        object? whereValue,
        CancellationToken cancellationToken)
    {
        var connection = _transaction?.Connection ?? await _dbContext.GetConnection(cancellationToken);
        var maxFieldName = maxField.GetPropertyName();
        var whereFieldName = whereField.GetPropertyName();

        var sql = BuildMax(maxFieldName, whereFieldName, whereValue);

        var parameters = whereValue is null ? null : new Dictionary<string, object?>
        {
            { $"@{whereFieldName}", whereValue },
        };

        return await connection.ExecuteScalarAsync<int?>(sql, parameters, _transaction) ?? default;
    }

    private static string BuildWhere(string field, object? value)
    {
        var isCollection = value is not string && value is System.Collections.IEnumerable;

        return
            isCollection
            ? $"{field} IN @{field}"
            : value is null ? $"{field} IS NULL" : $"{field}=@{field}";
    }

    private static string BuildWhereJson(string field, object? value)
    {
        var isCollection = value is not string && value is System.Collections.IEnumerable;

        return
            isCollection
            ? $"json_extract({nameof(Identity.JsonProperties)},'$.{field}') IN @{field}"
            : (value is null
                ? $"json_extract({nameof(Identity.JsonProperties)},'$.{field}') IS NULL"
                : $"json_extract({nameof(Identity.JsonProperties)},'$.{field}')=@{field}");
    }

    private static string BuildSelect(int count, string? field, object? value)
    {
        if (field is null)
        {
            return count > 0
                ? $"SELECT {_selectColumnNames} FROM {_tableName} LIMIT {count}"
                : $"SELECT {_selectColumnNames} FROM {_tableName}";
        }

        var where = BuildWhere(field, value);

        return count > 0
            ? $"SELECT {_selectColumnNames} FROM {_tableName} WHERE {where} LIMIT {count}"
            : $"SELECT {_selectColumnNames} FROM {_tableName} WHERE {where}";
    }

    private static string BuildSelectJson(int count, string field, object? value)
    {
        var where = BuildWhereJson(field, value);

        return count > 0
            ? $"SELECT {_selectColumnNames} FROM {_tableName} WHERE {where} LIMIT {count}"
            : $"SELECT {_selectColumnNames} FROM {_tableName} WHERE {where}";
    }

    private static string BuildCount(string? field, object? value)
    {
        if (field is null)
        {
            return $"SELECT COUNT(*) FROM {_tableName}";
        }

        var where = BuildWhere(field, value);

        return $"SELECT COUNT(*) FROM {_tableName} WHERE {where}";
    }

    private static string BuildMax(string maxField, string? whereField, object? whereValue)
    {
        if (whereField is null)
        {
            return $"SELECT MAX({maxField}) FROM {_tableName}";
        }

        var where = BuildWhere(whereField, whereValue);

        return $"SELECT MAX({maxField}) FROM {_tableName} WHERE {where}";
    }
}