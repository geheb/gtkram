using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace GtKram.Core.Migrations
{
    public partial class PreferredRegistration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredType",
                table: "bazaar_seller_registrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxArticleCount",
                table: "bazaar_sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset?>(
                name: "PickUpLabelsStartDate",
                table: "bazaar_events",
                type: "datetime(6)",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<DateTimeOffset?>(
                name: "PickUpLabelsEndDate",
                table: "bazaar_events",
                type: "datetime(6)",
                nullable: true,
                defaultValue: null);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredType",
                table: "bazaar_seller_registrations");

            migrationBuilder.DropColumn(
                name: "MaxArticleCount",
                table: "bazaar_sellers");

            migrationBuilder.DropColumn(
                name: "PickUpLabelsStartDate",
                table: "bazaar_events");

            migrationBuilder.DropColumn(
                name: "PickUpLabelsEndDate",
                table: "bazaar_events");
        }
    }
}
