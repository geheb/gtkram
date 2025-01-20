using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GtKram.Infrastructure.Persistence.Migrations
{
    public partial class Billing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bazaar_billings",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BazaarEventId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    UserId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_billings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bazaar_billings_bazaar_events_BazaarEventId",
                        column: x => x.BazaarEventId,
                        principalTable: "bazaar_events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bazaar_billings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "bazaar_billing_articles",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    AddedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BazaarBillingId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    BazaarSellerArticleId = table.Column<byte[]>(type: "binary(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_billing_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bazaar_billing_articles_bazaar_billings_BazaarBillingId",
                        column: x => x.BazaarBillingId,
                        principalTable: "bazaar_billings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bazaar_billing_articles_bazaar_seller_articles_BazaarSellerA~",
                        column: x => x.BazaarSellerArticleId,
                        principalTable: "bazaar_seller_articles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_billing_articles_BazaarBillingId",
                table: "bazaar_billing_articles",
                column: "BazaarBillingId");

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_billing_articles_BazaarSellerArticleId",
                table: "bazaar_billing_articles",
                column: "BazaarSellerArticleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_billings_BazaarEventId",
                table: "bazaar_billings",
                column: "BazaarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_billings_UserId",
                table: "bazaar_billings",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bazaar_billing_articles");

            migrationBuilder.DropTable(
                name: "bazaar_billings");
        }
    }
}
