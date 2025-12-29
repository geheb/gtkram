using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace GtKram.Infrastructure.Database;

internal sealed class SQLiteDbContext : IAsyncDisposable
{
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
            await connection.OpenAsync(cancellationToken);
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
