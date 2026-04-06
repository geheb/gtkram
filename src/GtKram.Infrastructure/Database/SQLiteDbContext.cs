using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace GtKram.Infrastructure.Database;

internal sealed class SQLiteDbContext : IAsyncDisposable
{
    private static readonly string[] _pragmas =
    [
        "PRAGMA journal_mode=WAL",
        "PRAGMA foreign_keys=1",
        "PRAGMA synchronous=NORMAL",
        "PRAGMA cache_size=-65536", // 64 MB
        "PRAGMA busy_timeout=5000", // 5 sec
        "PRAGMA mmap_size=1073741824", // 1 GB, higher than current db
        "PRAGMA secure_delete=1",
        "PRAGMA temp_store=MEMORY", // memory
    ];

    private SqliteConnection? _connection;
    private readonly string _connectionString;

    public SQLiteDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SQLite")!;
    }

    public async Task<DbConnection> GetConnection(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.CreateCollation("utf8_ci", (x, y) => string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase));
            await connection.OpenAsync(cancellationToken);

            foreach (var pragma in _pragmas)
            {
                await connection.ExecuteAsync(pragma);
            }

            _connection = connection;
        }

        return _connection;
    }

    public async Task<DbTransaction> BeginTransaction(CancellationToken cancellationToken)
    {
        var connection = await GetConnection(cancellationToken);
        return await connection.BeginTransactionAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => _connection?.DisposeAsync() ?? ValueTask.CompletedTask;
}
