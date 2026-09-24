using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data.Common;

namespace GtKram.Infrastructure.Database;

internal sealed class PostgresDbContext : IAsyncDisposable
{
    private NpgsqlConnection? _connection;
    private readonly string _connectionString;

    public PostgresDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("gtkram")!;
    }

    public async Task<DbConnection> GetConnection(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            var connection = new NpgsqlConnection(_connectionString);
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
