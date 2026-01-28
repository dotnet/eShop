using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "promotionseq",
                schema: "ordering",
                incrementBy: 10);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscount",
                schema: "ordering",
                table: "orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AppliedDiscount",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromotionId = table.Column<string>(type: "text", nullable: true),
                    PromotionName = table.Column<string>(type: "text", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedDiscount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppliedDiscount_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "numeric", nullable: true),
                    MinimumQuantity = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppliedDiscount_OrderId",
                schema: "ordering",
                table: "AppliedDiscount",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppliedDiscount",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "promotions",
                schema: "ordering");

            migrationBuilder.DropColumn(
                name: "TotalDiscount",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropSequence(
                name: "promotionseq",
                schema: "ordering");
        }
    }
}
