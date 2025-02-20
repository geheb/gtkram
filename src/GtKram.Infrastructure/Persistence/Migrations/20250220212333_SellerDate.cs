using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GtKram.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SellerDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "bazaar_seller_articles");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "bazaar_billings");

            migrationBuilder.RenameColumn(
                name: "AddedOn",
                table: "bazaar_billing_articles",
                newName: "CreatedOn");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                table: "bazaar_sellers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedOn",
                table: "bazaar_sellers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "bazaar_sellers");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "bazaar_sellers");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "bazaar_billing_articles",
                newName: "AddedOn");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "bazaar_seller_articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "bazaar_billings",
                type: "decimal(8,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
