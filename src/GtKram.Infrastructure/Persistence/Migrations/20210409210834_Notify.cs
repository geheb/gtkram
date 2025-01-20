using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GtKram.Infrastructure.Persistence.Migrations
{
    public partial class Notify : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_notifications",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    UserId = table.Column<byte[]>(type: "binary(16)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    SentOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CallbackUrl = table.Column<string>(type: "longtext", nullable: true),
                    ReferenceId = table.Column<byte[]>(type: "binary(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_notifications_ReferenceId_Type",
                table: "account_notifications",
                columns: new[] { "ReferenceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_account_notifications_UserId_Type_CreatedOn",
                table: "account_notifications",
                columns: new[] { "UserId", "Type", "CreatedOn" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_notifications");
        }
    }
}
