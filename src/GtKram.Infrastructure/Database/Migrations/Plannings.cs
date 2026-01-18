using FluentMigrator;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Migrations;

[Migration(20260114)]
public sealed class Plannings : Migration
{
    public override void Up()
    {
        const string table = TableNames.Plannings;

        Create.Table(table)
            .WithColumn(nameof(Planning.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Planning.Created)).AsString()
            .WithColumn(nameof(Planning.Updated)).AsString().Nullable()
            .WithColumn(nameof(Planning.JsonProperties)).AsString()
            .WithColumn(nameof(Planning.JsonVersion)).AsInt32()
            .WithColumn(nameof(Planning.EventId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id));

        Create.Index($"IX_{table}_{nameof(Planning.EventId)}")
            .OnTable(table)
            .OnColumn(nameof(Planning.EventId));
    }

    public override void Down()
    {
    }
}
