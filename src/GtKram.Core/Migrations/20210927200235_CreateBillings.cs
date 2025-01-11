using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GtKram.Core.Migrations
{
    public partial class CreateBillings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreateBillings",
                table: "bazaar_sellers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { new byte[] { 253, 149, 98, 54, 168, 231, 45, 65, 129, 208, 78, 66, 62, 219, 199, 84 }, "274A03E0-CB30-4324-9A27-90A5915E6C84", "billing", "BILLING" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new byte[] { 253, 149, 98, 54, 168, 231, 45, 65, 129, 208, 78, 66, 62, 219, 199, 84 });

            migrationBuilder.DropColumn(
                name: "CanCreateBillings",
                table: "bazaar_sellers");
        }
    }
}
