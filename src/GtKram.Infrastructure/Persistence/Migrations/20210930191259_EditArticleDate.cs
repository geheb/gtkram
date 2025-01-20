using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GtKram.Infrastructure.Persistence.Migrations
{
    public partial class EditArticleDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EditArticleEndDate",
                table: "bazaar_events",
                type: "datetime(6)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditArticleEndDate",
                table: "bazaar_events");
        }
    }
}
