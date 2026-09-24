using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Migrations;

[Migration(20261001)]
public sealed class Initial : Migration
{
    public override void Up()
    {
        Create.Schema(TableSchemas.Infra);
        Create.Schema(TableSchemas.Events);

        CreateIdentitites();
        CreateEmailQueues();
        CreateEvents();
        CreateSellers();
        CreateSellerRegistrations();
        CreateArticles();
        CreateCheckouts();
        CreatePlannings();
    }

    public override void Down()
    {
    }

    private void CreateIdentitites()
    {
        CreateTableWithJson(TableNames.Identities, TableSchemas.Infra);

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{TableNames.Identities}_normalizedemail
            ON "{TableSchemas.Infra}"."{TableNames.Identities}" (("{nameof(JsonEntity<>.Data)}"->>'{nameof(IdentityValues.NormalizedEmail)}'))
            WHERE "{nameof(JsonEntity<>.Data)}"->>'{nameof(IdentityValues.NormalizedEmail)}' IS NOT NULL;
            """);

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{TableNames.Identities}_normalizedusername
            ON "{TableSchemas.Infra}"."{TableNames.Identities}" (("{nameof(JsonEntity<>.Data)}"->>'{nameof(IdentityValues.NormalizedUserName)}'))
            WHERE "{nameof(JsonEntity<>.Data)}"->>'{nameof(IdentityValues.NormalizedUserName)}' IS NOT NULL;
            """);
    }

    private void CreateEmailQueues()
    {
        CreateTableWithJson(TableNames.EmailQueues, TableSchemas.Infra);
    }

    private void CreateEvents()
    {
        CreateTableWithJson(TableNames.Events, TableSchemas.Events);
    }

    private void CreateSellers()
    {
        const string table = TableNames.Sellers;
        const string schema = TableSchemas.Events;

        CreateTableWithJson(table, schema)
            .WithColumn(nameof(SellerValues.EventId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(SellerValues.EventId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Events}", schema, TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(SellerValues.IdentityId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(SellerValues.IdentityId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Identities}", TableSchemas.Infra, TableNames.Identities, nameof(Identity.Id));

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{table}_eventid_identityid
            ON "{schema}"."{table}" (
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerValues.EventId)}'),
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerValues.IdentityId)}')
            )
            WHERE 
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerValues.EventId)}' IS NOT NULL AND
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerValues.IdentityId)}' IS NOT NULL
            """);
    }

    private void CreateSellerRegistrations()
    {
        const string table = TableNames.SellerRegistrations;
        const string schema = TableSchemas.Events;

        CreateTableWithJson(table, schema)
            .WithColumn(nameof(SellerRegistrationValues.EventId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(SellerRegistrationValues.EventId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Events}", schema, TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(SellerRegistrationValues.SellerId)).AsGuid().Nullable()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(SellerRegistrationValues.SellerId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Sellers}", schema, TableNames.Sellers, nameof(Seller.Id));

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{table}_eventid_sellerid
            ON "{schema}"."{table}" (
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.EventId)}'),
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.SellerId)}')
            )
            WHERE 
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.EventId)}' IS NOT NULL AND
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.SellerId)}' IS NOT NULL
            """);

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{table}_eventid_normalizedemail
            ON "{schema}"."{table}" (
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.EventId)}'),
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.NormalizedEmail)}')
            )
            WHERE 
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.EventId)}' IS NOT NULL AND
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(SellerRegistrationValues.NormalizedEmail)}' IS NOT NULL
            """);
    }

    private void CreateArticles()
    {
        const string table = TableNames.Articles;
        const string schema = TableSchemas.Events;

        CreateTableWithJson(table, schema)
            .WithColumn(nameof(ArticleValues.SellerId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(ArticleValues.SellerId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Sellers}", schema, TableNames.Sellers, nameof(Seller.Id));

        Execute.Sql(
            $"""
            CREATE UNIQUE INDEX uix_{table}_sellerid_labelnumber
            ON "{schema}"."{table}" (
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(ArticleValues.SellerId)}'),
                ("{nameof(JsonEntity<>.Data)}"->>'{nameof(ArticleValues.LabelNumber)}')
            )
            WHERE 
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(ArticleValues.SellerId)}' IS NOT NULL AND
                "{nameof(JsonEntity<>.Data)}"->>'{nameof(ArticleValues.LabelNumber)}' IS NOT NULL
            """);
    }

    private void CreateCheckouts()
    {
        const string table = TableNames.Checkouts;
        const string schema = TableSchemas.Events;

        CreateTableWithJson(table, schema)
            .WithColumn(nameof(CheckoutValues.EventId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(CheckoutValues.EventId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Events}", schema, TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(CheckoutValues.IdentityId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(CheckoutValues.IdentityId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Identities}", TableSchemas.Infra, TableNames.Identities, nameof(Identity.Id));
    }

    private void CreatePlannings()
    {
        const string table = TableNames.Plannings;
        const string schema = TableSchemas.Events;

        CreateTableWithJson(table, schema)
            .WithColumn(nameof(PlanningValues.EventId)).AsGuid()
                .Computed($"(\"{nameof(JsonEntity<>.Data)}\"->>'{nameof(PlanningValues.EventId)}')::uuid", true)
                .ForeignKey($"fk_{table}_{TableNames.Events}", schema, TableNames.Events, nameof(Event.Id));
    }

    private ICreateTableWithColumnSyntax CreateTableWithJson(string name, string schema) =>
         Create.Table(name)
            .InSchema(schema)
            .WithColumn(nameof(JsonEntity<>.Id)).AsGuid().PrimaryKey($"pk_{name}")
            .WithColumn(nameof(JsonEntity<>.Created)).AsDateTimeOffset()
            .WithColumn(nameof(JsonEntity<>.Updated)).AsDateTimeOffset().Nullable()
            .WithColumn(nameof(JsonEntity<>.Data)).AsCustom("jsonb").NotNullable()
            .WithColumn(nameof(JsonEntity<>.Version)).AsInt32();
}
