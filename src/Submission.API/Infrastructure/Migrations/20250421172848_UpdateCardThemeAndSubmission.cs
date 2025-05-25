using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Submission.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCardThemeAndSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "IntegrationEventLog");

            migrationBuilder.DropColumn(
                name: "EventTypeName",
                table: "IntegrationEventLog");

            migrationBuilder.AddColumn<string>(
                name: "Kanji",
                table: "CardTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Accent",
                table: "CardThemes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CardThemes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Primary",
                table: "CardThemes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Secondary",
                table: "CardThemes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kanji",
                table: "CardTypes");

            migrationBuilder.DropColumn(
                name: "Accent",
                table: "CardThemes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CardThemes");

            migrationBuilder.DropColumn(
                name: "Primary",
                table: "CardThemes");

            migrationBuilder.DropColumn(
                name: "Secondary",
                table: "CardThemes");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "IntegrationEventLog",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventTypeName",
                table: "IntegrationEventLog",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
