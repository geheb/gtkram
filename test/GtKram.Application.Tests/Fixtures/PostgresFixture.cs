using Npgsql;
using Testcontainers.PostgreSql;

namespace GtKram.Application.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncDisposable
{
    private static readonly Lazy<PostgresFixture> _instance = new Lazy<PostgresFixture>(() => new());

    public static PostgresFixture Instance => _instance.Value;

    private readonly PostgreSqlContainer _container;

    private PostgresFixture()
    {
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await _container.StartAsync(cancellationToken);
    }

    public async Task<string> CreateDatabase()
    {
        var databaseName = $"test_{Guid.NewGuid():N}";

        var connectionString = _container.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";

        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = databaseName,
            MaxPoolSize= 10,
        }.ConnectionString;
    }

    public ValueTask DisposeAsync() =>
        _container.DisposeAsync();
}