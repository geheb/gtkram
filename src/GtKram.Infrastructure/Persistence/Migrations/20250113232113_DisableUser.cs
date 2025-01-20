using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GtKram.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DisableUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledOn",
                table: "users",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledOn",
                table: "users");
        }
    }
}
