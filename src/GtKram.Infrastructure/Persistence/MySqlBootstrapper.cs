using Dapper;
using GtKram.Infrastructure.Persistence.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace GtKram.Infrastructure.Persistence;

internal sealed class MySqlBootstrapper
{
    private readonly MySqlDbContext _dbContext;

    public MySqlBootstrapper(
        MySqlDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Bootstrap(CancellationToken cancellationToken)
    {
        var connection = await _dbContext.GetConnection(cancellationToken);

        const string typeVarchar256 = "VARCHAR(256)";
        const string typeVarchar64 = "VARCHAR(64)";
        const string typeUtcDateTime = "DATETIME(6)";
        const string typeGuid = "CHAR(32)";
        const string typeInt = "INT";

        var identity = new TableContext<Identity>(connection);
        await identity.CreateTable();
        await identity.AddJsonColumn(e => e.Email, typeVarchar256);
        await identity.AddJsonColumn(e => e.UserName, typeVarchar64);
        await identity.AddJsonColumn(e => e.Disabled, typeUtcDateTime);
        await identity.AddUniqueIndex(e => e.Email);
        await identity.AddUniqueIndex(e => e.UserName);

        var emailQueue = new TableContext<EmailQueue>(connection);
        await emailQueue.CreateTable();
        await emailQueue.AddJsonColumn(e => e.Sent, typeUtcDateTime);

        var @event = new TableContext<Event>(connection);
        await @event.CreateTable();

        var sellerRegistrations = new TableContext<SellerRegistration>(connection);
        await sellerRegistrations.CreateTable();
        await sellerRegistrations.AddJsonColumn(e => e.EventId, typeGuid);
        await sellerRegistrations.AddJsonColumn(e => e.SellerId, typeGuid);
        await sellerRegistrations.AddJsonColumn(e => e.Email, typeVarchar256);
        await sellerRegistrations.AddIndex(e => e.EventId);
        await sellerRegistrations.AddIndex(e => e.SellerId);

        var sellers = new TableContext<Seller>(connection);
        await sellers.CreateTable();
        await sellers.AddJsonColumn(e => e.EventId, typeGuid);
        await sellers.AddJsonColumn(e => e.UserId, typeGuid);
        await sellers.AddJsonColumn(e => e.SellerNumber, typeInt);
        await sellers.AddIndex(e => e.EventId);
        await sellers.AddIndex(e => e.UserId);

        var articles = new TableContext<Article>(connection);
        await articles.CreateTable();
        await articles.AddJsonColumn(e => e.SellerId, typeGuid);
        await articles.AddJsonColumn(e => e.LabelNumber, typeInt);
        await articles.AddIndex(e => e.SellerId);

        var checkouts = new TableContext<Checkout>(connection);
        await checkouts.CreateTable();
        await checkouts.AddJsonColumn(e => e.EventId, typeGuid);
        await checkouts.AddJsonColumn(e => e.UserId, typeGuid);
        await checkouts.AddIndex(e => e.EventId);
        await checkouts.AddIndex(e => e.UserId);
    }

    private sealed class TableContext<T>(IDbConnection connection) where T : IEntity
    {
        private static readonly string _tableName;
        private readonly IDbConnection _connection = connection;

        private const string _createTable = """
        CREATE TABLE IF NOT EXISTS {0}
        (
        	Id BINARY(16) NOT NULL,
            Created DATETIME(6) NOT NULL,
            Modified DATETIME(6) NULL,
            Version INT NOT NULL,
        	Json JSON NOT NULL

            CHECK (JSON_VALID(Json)),
        	PRIMARY KEY (Id)
        ) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
        """;

        private const string _addColumn = "ALTER TABLE `{0}` ADD COLUMN IF NOT EXISTS {1} {2} AS (JSON_VALUE(Json, '{3}'));";
        private const string _showIndex = "SHOW INDEX FROM `{0}` WHERE `Key_name`='{1}';";
        private const string _addUniqueIndex = "ALTER TABLE `{0}` ADD UNIQUE INDEX `{1}` ({2});";
        private const string _addIndex = "ALTER TABLE `{0}` ADD INDEX `{1}` ({2});";

        static TableContext()
        {
            _tableName = typeof(T).GetCustomAttribute<TableAttribute>()!.Name;
        }

        internal async Task CreateTable()
        {
            var sql = string.Format(_createTable, _tableName);
            await _connection.ExecuteAsync(sql);
        }

        internal async Task AddJsonColumn(Expression<Func<T, object?>> field, string dataType)
        {
            var path = field.GetPropertyName();
            var sql = string.Format(_addColumn, _tableName, "_" + path, dataType, "$." + path);
            await _connection.ExecuteAsync(sql);
        }

        internal async Task AddUniqueIndex(Expression<Func<T, object?>> field)
        {
            var path = field.GetPropertyName();
            var sql = string.Format(_showIndex, _tableName, "ix_" + path);
            var hasIndex = (await _connection.QueryAsync(sql)).Any();
            if (hasIndex)
            {
                return;
            }

            sql = string.Format(_addUniqueIndex, _tableName, "ix_" + path, $"`_{path}`");
            await _connection.ExecuteAsync(sql);
        }

        internal async Task AddIndex(params Expression<Func<T, object?>>[] fields)
        {
            var names = fields.Select(f => f.GetPropertyName()).ToArray();

            var name = string.Join("_", names);
            var sql = string.Format(_showIndex, _tableName, "ix_" + name);
            var hasIndex = (await _connection.QueryAsync(sql)).Any();
            if (hasIndex)
            {
                return;
            }

            var columns = string.Join(",", names.Select(p => $"`_{p}`"));
            sql = string.Format(_addIndex, _tableName, "ix_" + name, columns);
            await _connection.ExecuteAsync(sql);
        }
    }
}
