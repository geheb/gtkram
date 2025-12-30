using FluentMigrator;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

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
            .WithColumn(nameof(Identity.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Identity.Created)).AsString()
            .WithColumn(nameof(Identity.Updated)).AsString().Nullable()
            .WithColumn(nameof(Identity.JsonProperties)).AsString()
            .WithColumn(nameof(Identity.JsonVersion)).AsInt32()
            .WithColumn(nameof(Identity.Email)).AsString(256).Unique();
    }


    private void CreateEmailQueues()
    {
        const string table = TableNames.EmailQueues;

        Create.Table(table)
            .WithColumn(nameof(EmailQueue.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(EmailQueue.Created)).AsString()
            .WithColumn(nameof(EmailQueue.Updated)).AsString().Nullable()
            .WithColumn(nameof(EmailQueue.JsonProperties)).AsString()
            .WithColumn(nameof(EmailQueue.JsonVersion)).AsInt32()
            .WithColumn(nameof(EmailQueue.IsSent)).AsInt32();

        Create.Index($"IX_{table}_{nameof(EmailQueue.IsSent)}")
            .OnTable(table)
            .OnColumn(nameof(EmailQueue.IsSent));
    }

    private void CreateEvents()
    {
        Create.Table(TableNames.Events)
            .WithColumn(nameof(Event.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Event.Created)).AsString()
            .WithColumn(nameof(Event.Updated)).AsString().Nullable()
            .WithColumn(nameof(Event.JsonProperties)).AsString()
            .WithColumn(nameof(Event.JsonVersion)).AsInt32();
    }

    private void CreateSellers()
    {
        const string table = TableNames.Sellers;

        Create.Table(table)
            .WithColumn(nameof(Seller.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Seller.Created)).AsString()
            .WithColumn(nameof(Seller.Updated)).AsString().Nullable()
            .WithColumn(nameof(Seller.JsonProperties)).AsString()
            .WithColumn(nameof(Seller.JsonVersion)).AsInt32()
            .WithColumn(nameof(Seller.SellerNumber)).AsInt32()
            .WithColumn(nameof(Seller.EventId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(Seller.IdentityId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Identities}", TableNames.Identities, nameof(Identity.Id));

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
            .WithColumn(nameof(SellerRegistration.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(SellerRegistration.Created)).AsString()
            .WithColumn(nameof(SellerRegistration.Updated)).AsString().Nullable()
            .WithColumn(nameof(SellerRegistration.JsonProperties)).AsString()
            .WithColumn(nameof(SellerRegistration.JsonVersion)).AsInt32()
            .WithColumn(nameof(SellerRegistration.EventId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(SellerRegistration.SellerId)).AsString(36).Nullable()
                .ForeignKey($"FK_{table}_{TableNames.Sellers}", TableNames.Sellers, nameof(Seller.Id));

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
            .WithColumn(nameof(Article.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Article.Created)).AsString()
            .WithColumn(nameof(Article.Updated)).AsString().Nullable()
            .WithColumn(nameof(Article.JsonProperties)).AsString()
            .WithColumn(nameof(Article.JsonVersion)).AsInt32()
            .WithColumn(nameof(Article.SellerId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Sellers}", TableNames.Sellers, nameof(Seller.Id))
            .WithColumn(nameof(Article.LabelNumber)).AsInt32();

        Create.Index($"IX_{table}_{nameof(Article.SellerId)}")
            .OnTable(table)
            .OnColumn(nameof(Article.SellerId));
    }

    private void CreateCheckouts()
    {
        const string table = TableNames.Checkouts;

        Create.Table(table)
            .WithColumn(nameof(Checkout.Id)).AsString(36).PrimaryKey()
            .WithColumn(nameof(Checkout.Created)).AsString()
            .WithColumn(nameof(Checkout.Updated)).AsString().Nullable()
            .WithColumn(nameof(Checkout.JsonProperties)).AsString()
            .WithColumn(nameof(Checkout.JsonVersion)).AsInt32()
            .WithColumn(nameof(Checkout.EventId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Events}", TableNames.Events, nameof(Event.Id))
            .WithColumn(nameof(Checkout.IdentityId)).AsString(36)
                .ForeignKey($"FK_{table}_{TableNames.Identities}", TableNames.Identities, nameof(Identity.Id));

        Create.Index($"IX_{table}_{nameof(Checkout.EventId)}")
            .OnTable(table)
            .OnColumn(nameof(Checkout.EventId));

        Create.Index($"IX_{table}_{nameof(Checkout.IdentityId)}")
            .OnTable(table)
            .OnColumn(nameof(Checkout.IdentityId));
    }
}
