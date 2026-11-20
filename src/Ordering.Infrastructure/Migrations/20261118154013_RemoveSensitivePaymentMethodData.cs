using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveSensitivePaymentMethodData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_paymentmethods_cardtypes_CardTypeId",
            schema: "ordering",
            table: "paymentmethods");

        migrationBuilder.DropTable(
            name: "cardtypes",
            schema: "ordering");

        migrationBuilder.DropIndex(
            name: "IX_paymentmethods_CardTypeId",
            schema: "ordering",
            table: "paymentmethods");

        migrationBuilder.AddColumn<string>(
            name: "PaymentToken",
            schema: "ordering",
            table: "paymentmethods",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.Sql("UPDATE ordering.paymentmethods SET \"PaymentToken\" = 'legacy-' || \"Id\"::text;");

        migrationBuilder.AlterColumn<string>(
            name: "PaymentToken",
            schema: "ordering",
            table: "paymentmethods",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

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

        migrationBuilder.DropColumn(
            name: "CardHolderName",
            schema: "ordering",
            table: "paymentmethods");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentToken",
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
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "Expiration",
            schema: "ordering",
            table: "paymentmethods",
            type: "timestamp with time zone",
            maxLength: 25,
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.CreateTable(
            name: "cardtypes",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cardtypes", x => x.Id);
            });

        migrationBuilder.InsertData(
            schema: "ordering",
            table: "cardtypes",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
                { 1, "Amex" },
                { 2, "Visa" },
                { 3, "MasterCard" }
            });

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
