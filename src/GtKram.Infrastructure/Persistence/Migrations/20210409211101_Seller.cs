using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GtKram.Infrastructure.Persistence.Migrations
{
    public partial class Seller : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bazaar_events",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Address = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    MaxSellers = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    Commission = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    RegisterStartDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    RegisterEndDate = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsRegistrationsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bazaar_sellers",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    BazaarEventId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    UserId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    SellerNumber = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_sellers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bazaar_sellers_bazaar_events_BazaarEventId",
                        column: x => x.BazaarEventId,
                        principalTable: "bazaar_events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bazaar_sellers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "bazaar_seller_articles",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    BazaarSellerId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    LabelNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Size = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_seller_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bazaar_seller_articles_bazaar_sellers_BazaarSellerId",
                        column: x => x.BazaarSellerId,
                        principalTable: "bazaar_sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "bazaar_seller_registrations",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    BazaarEventId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Clothing = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true),
                    Accepted = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    BazaarSellerId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bazaar_seller_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bazaar_seller_registrations_bazaar_events_BazaarEventId",
                        column: x => x.BazaarEventId,
                        principalTable: "bazaar_events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bazaar_seller_registrations_bazaar_sellers_BazaarSellerId",
                        column: x => x.BazaarSellerId,
                        principalTable: "bazaar_sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_seller_articles_BazaarSellerId",
                table: "bazaar_seller_articles",
                column: "BazaarSellerId");

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_seller_registrations_BazaarEventId_Email",
                table: "bazaar_seller_registrations",
                columns: new[] { "BazaarEventId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_seller_registrations_BazaarSellerId",
                table: "bazaar_seller_registrations",
                column: "BazaarSellerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_sellers_BazaarEventId_UserId",
                table: "bazaar_sellers",
                columns: new[] { "BazaarEventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bazaar_sellers_UserId",
                table: "bazaar_sellers",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bazaar_seller_articles");

            migrationBuilder.DropTable(
                name: "bazaar_seller_registrations");

            migrationBuilder.DropTable(
                name: "bazaar_sellers");

            migrationBuilder.DropTable(
                name: "bazaar_events");
        }
    }
}
