using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GtKram.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_queue",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "binary(16)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Recipient = table.Column<string>(type: "longtext", nullable: false),
                    Subject = table.Column<string>(type: "longtext", nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false),
                    AttachmentBlob = table.Column<byte[]>(type: "longblob", nullable: true),
                    AttachmentName = table.Column<string>(type: "longtext", nullable: true),
                    AttachmentMimeType = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_queue", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_queue");
        }
    }
}
