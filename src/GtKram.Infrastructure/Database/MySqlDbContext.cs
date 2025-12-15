using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Data.Common;

namespace GtKram.Infrastructure.Database;

internal sealed class MySqlDbContext : IAsyncDisposable
{
    private MySqlConnection? _connection;
    private readonly string _connectionString;

    public MySqlDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySql")!;
    }

    public async Task<DbConnection> GetConnection(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            _connection = connection;
        }

        return _connection;
    }

    public ValueTask DisposeAsync() => _connection?.DisposeAsync() ?? ValueTask.CompletedTask;
}
