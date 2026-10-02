using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.Identity.API.Data.Migrations
{
    public partial class RemoveSensitiveCardProfileData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Expiration",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SecurityNumber",
                table: "AspNetUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Expiration",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecurityNumber",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
