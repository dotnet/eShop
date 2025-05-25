using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_City",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Address_State",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Address_ZipCode",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Content",
                schema: "ordering",
                table: "IntegrationEventLog");

            migrationBuilder.DropColumn(
                name: "EventTypeName",
                schema: "ordering",
                table: "IntegrationEventLog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_State",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_ZipCode",
                schema: "ordering",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                schema: "ordering",
                table: "IntegrationEventLog",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventTypeName",
                schema: "ordering",
                table: "IntegrationEventLog",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
