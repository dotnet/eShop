using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    public partial class RemoveSensitivePaymentMethodData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_paymentmethods_cardtypes_CardTypeId",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.DropIndex(
                name: "IX_paymentmethods_CardTypeId",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.DropColumn(
                name: "Expiration",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodId",
                schema: "ordering",
                table: "paymentmethods",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                schema: "ordering",
                table: "paymentmethods");

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                schema: "ordering",
                table: "paymentmethods",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                schema: "ordering",
                table: "paymentmethods",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CardTypeId",
                schema: "ordering",
                table: "paymentmethods",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "Expiration",
                schema: "ordering",
                table: "paymentmethods",
                type: "timestamp with time zone",
                maxLength: 25,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.CreateIndex(
                name: "IX_paymentmethods_CardTypeId",
                schema: "ordering",
                table: "paymentmethods",
                column: "CardTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_paymentmethods_cardtypes_CardTypeId",
                schema: "ordering",
                table: "paymentmethods",
                column: "CardTypeId",
                principalSchema: "ordering",
                principalTable: "cardtypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
