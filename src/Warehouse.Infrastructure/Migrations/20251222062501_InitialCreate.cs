using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.Warehouse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "warehouse");

            migrationBuilder.CreateSequence(
                name: "inventoryseq",
                schema: "warehouse",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "warehouseseq",
                schema: "warehouse",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_inventory",
                schema: "warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    CatalogItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_warehouse_inventory_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "warehouse",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_inventory_CatalogItemId",
                schema: "warehouse",
                table: "warehouse_inventory",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_inventory_WarehouseId_CatalogItemId",
                schema: "warehouse",
                table: "warehouse_inventory",
                columns: new[] { "WarehouseId", "CatalogItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warehouse_inventory",
                schema: "warehouse");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "warehouse");

            migrationBuilder.DropSequence(
                name: "inventoryseq",
                schema: "warehouse");

            migrationBuilder.DropSequence(
                name: "warehouseseq",
                schema: "warehouse");
        }
    }
}
