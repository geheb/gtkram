using FluentMigrator;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;
using GtKram.Infrastructure.Repositories;
using System.Data;

namespace GtKram.Infrastructure.Database.Migrations;

[Migration(20251218)]
public sealed class Initial : Migration
{
    public override void Up()
    {
        CreateIdentitites();
        CreateEmailQueues();
        CreateEvents();
        CreateSellers();
        CreateSellerRegistrations();
        CreateArticles();
        CreateCheckouts();
    }

    public override void Down()
    {
    }

    private void CreateIdentitites()
    {
        Create.Table(TableNames.Identities)
            .WithColumn(nameof(Identity.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(Identity.Created)).AsDateTime()
            .WithColumn(nameof(Identity.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(Identity.JsonProperties)).AsString()
            .WithColumn(nameof(Identity.JsonVersion)).AsInt32()
            .WithColumn(nameof(Identity.Email)).AsString(256).Unique();
    }


    private void CreateEmailQueues()
    {
        const string table = TableNames.EmailQueues;

        Create.Table(table)
            .WithColumn(nameof(EmailQueue.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(EmailQueue.Created)).AsDateTime()
            .WithColumn(nameof(EmailQueue.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(EmailQueue.JsonProperties)).AsString()
            .WithColumn(nameof(EmailQueue.JsonVersion)).AsInt32()
            .WithColumn(nameof(EmailQueue.IsSent)).AsBoolean();

        Create.Index($"IX_{table}_{nameof(EmailQueue.IsSent)}")
            .OnTable(table)
            .OnColumn(nameof(EmailQueue.IsSent));
    }

    private void CreateEvents()
    {
        Create.Table(TableNames.Events)
            .WithColumn(nameof(Event.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(Event.Created)).AsDateTime()
            .WithColumn(nameof(Event.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(Event.JsonProperties)).AsString()
            .WithColumn(nameof(Event.JsonVersion)).AsInt32();
    }

    private void CreateSellers()
    {
        const string table = TableNames.Sellers;

        Create.Table(table)
            .WithColumn(nameof(Seller.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(Seller.Created)).AsDateTime()
            .WithColumn(nameof(Seller.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(Seller.JsonProperties)).AsString()
            .WithColumn(nameof(Seller.JsonVersion)).AsInt32()
            .WithColumn(nameof(Seller.SellerNumber)).AsInt32()
            .WithColumn(nameof(Seller.EventId)).AsGuid()
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(Seller.IdentityId)).AsGuid()
                .ForeignKey($"FK_{table}_{TableNames.Identities}", TableNames.Identities, nameof(Identity.Id));

        /*Create.ForeignKey($"FK_{table}_{TableNames.Events}")
            .FromTable(table).ForeignColumn(nameof(Seller.EventId))
            .ToTable(TableNames.Events).PrimaryColumn(nameof(Event.Id))
            .OnDelete(Rule.None);

        Create.ForeignKey($"FK_{table}_{TableNames.Identities}")
            .FromTable(table).ForeignColumn(nameof(Seller.IdentityId))
            .ToTable(TableNames.Identities).PrimaryColumn(nameof(Identity.Id))
            .OnDelete(Rule.None);*/

        Create.Index($"IX_{table}_{nameof(Seller.EventId)}")
            .OnTable(table)
            .OnColumn(nameof(Seller.EventId));

        Create.Index($"IX_{table}_{nameof(Seller.IdentityId)}")
            .OnTable(table)
            .OnColumn(nameof(Seller.IdentityId));
    }

    private void CreateSellerRegistrations()
    {
        const string table = TableNames.SellerRegistrations;

        Create.Table(table)
            .WithColumn(nameof(SellerRegistration.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(SellerRegistration.Created)).AsDateTime()
            .WithColumn(nameof(SellerRegistration.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(SellerRegistration.JsonProperties)).AsString()
            .WithColumn(nameof(SellerRegistration.JsonVersion)).AsInt32()
            .WithColumn(nameof(SellerRegistration.EventId)).AsGuid()
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(SellerRegistration.SellerId)).AsGuid().Nullable()
                .ForeignKey($"FK_{table}_{TableNames.Sellers}", TableNames.Sellers, nameof(Seller.Id));

        /*Create.ForeignKey($"FK_{table}_{TableNames.Events}")
            .FromTable(table).ForeignColumn(nameof(SellerRegistration.EventId))
            .ToTable(TableNames.Events).PrimaryColumn(nameof(Event.Id))
            .OnDelete(Rule.None);

        Create.ForeignKey($"FK_{table}_{table}")
            .FromTable(table).ForeignColumn(nameof(SellerRegistration.SellerId))
            .ToTable(TableNames.Sellers).PrimaryColumn(nameof(Seller.Id))
            .OnDelete(Rule.None);*/

        Create.Index($"IX_{table}_{nameof(SellerRegistration.EventId)}")
            .OnTable(table)
            .OnColumn(nameof(SellerRegistration.EventId));

        Create.Index($"IX_{table}_{nameof(SellerRegistration.SellerId)}")
            .OnTable(table)
            .OnColumn(nameof(SellerRegistration.SellerId));
    }

    private void CreateArticles()
    {
        const string table = TableNames.Articles;

        Create.Table(table)
            .WithColumn(nameof(Article.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(Article.Created)).AsDateTime()
            .WithColumn(nameof(Article.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(Article.JsonProperties)).AsString()
            .WithColumn(nameof(Article.JsonVersion)).AsInt32()
            .WithColumn(nameof(Article.SellerId)).AsGuid()
                .ForeignKey($"FK_{table}_{TableNames.Sellers}", TableNames.Sellers, nameof(Seller.Id))
            .WithColumn(nameof(Article.LabelNumber)).AsInt32();

        /*Create.ForeignKey($"FK_{table}_{table}")
            .FromTable(table).ForeignColumn(nameof(Article.SellerId))
            .ToTable(TableNames.Sellers).PrimaryColumn(nameof(Seller.Id))
            .OnDelete(Rule.None);*/

        Create.Index($"IX_{table}_{nameof(Article.SellerId)}")
            .OnTable(table)
            .OnColumn(nameof(Article.SellerId));
    }

    private void CreateCheckouts()
    {
        const string table = TableNames.Checkouts;

        Create.Table(table)
            .WithColumn(nameof(Checkout.Id)).AsGuid().PrimaryKey()
            .WithColumn(nameof(Checkout.Created)).AsDateTime()
            .WithColumn(nameof(Checkout.Updated)).AsDateTime().Nullable()
            .WithColumn(nameof(Checkout.JsonProperties)).AsString()
            .WithColumn(nameof(Checkout.JsonVersion)).AsInt32()
            .WithColumn(nameof(Checkout.EventId)).AsGuid()
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(Checkout.IdentityId)).AsInt32()
                .ForeignKey($"FK_{table}_{TableNames.Identities}", TableNames.Identities, nameof(Identity.Id));

        /*Create.ForeignKey($"FK_{table}_{TableNames.Events}")
            .FromTable(table).ForeignColumn(nameof(Checkout.EventId))
            .ToTable(TableNames.Events).PrimaryColumn(nameof(Event.Id))
            .OnDelete(Rule.None);

        Create.ForeignKey($"FK_{table}_{TableNames.Identities}")
            .FromTable(table).ForeignColumn(nameof(Checkout.IdentityId))
            .ToTable(TableNames.Identities).PrimaryColumn(nameof(Identity.Id))
            .OnDelete(Rule.None);*/

        Create.Index($"IX_{table}_{nameof(Checkout.EventId)}")
            .OnTable(table)
            .OnColumn(nameof(Checkout.EventId));

        Create.Index($"IX_{table}_{nameof(Checkout.IdentityId)}")
            .OnTable(table)
            .OnColumn(nameof(Checkout.IdentityId));
    }
}
