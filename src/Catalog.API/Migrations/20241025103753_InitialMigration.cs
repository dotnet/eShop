using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace Catalog.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "CatalogFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    TitleEN = table.Column<string>(type: "text", nullable: true),
                    TitleDE = table.Column<string>(type: "text", nullable: true),
                    ValueEN = table.Column<string>(type: "text", nullable: true),
                    ValueDE = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    NameEN = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NameDE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    DescriptionDE = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    DescriptionEN = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ProductWeight = table.Column<string>(type: "text", nullable: true),
                    ProducctType = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<string>(type: "text", nullable: true),
                    CategoryNameEN = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CategoryNameDE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductSKU = table.Column<string>(type: "text", nullable: true),
                    ProductKeyEN = table.Column<string>(type: "text", nullable: true),
                    ProductKenDE = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginPrice = table.Column<string>(type: "text", nullable: true),
                    SuggestSellPrice = table.Column<string>(type: "text", nullable: true),
                    ListedNum = table.Column<int>(type: "integer", nullable: false),
                    PictureFileName = table.Column<string>(type: "text", nullable: true),
                    PackingWeight = table.Column<string>(type: "text", nullable: true),
                    PackingNameEN = table.Column<string>(type: "text", nullable: true),
                    PackingNameDE = table.Column<string>(type: "text", nullable: true),
                    PackingNameSetEN = table.Column<string>(type: "text", nullable: true),
                    PackingNameSetDE = table.Column<string>(type: "text", nullable: true),
                    CatalogTypeId = table.Column<int>(type: "integer", nullable: false),
                    CatalogBrandId = table.Column<int>(type: "integer", nullable: false),
                    AvailableStock = table.Column<int>(type: "integer", nullable: false),
                    RestockThreshold = table.Column<int>(type: "integer", nullable: false),
                    MaxStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(384)", nullable: true),
                    OnReorder = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogKits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    NameDE = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogKits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEventLog",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventTypeName = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    TimesSent = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEventLog", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "CatalogFeatureCatalogItem",
                columns: table => new
                {
                    CatalogFeaturesId = table.Column<int>(type: "integer", nullable: false),
                    CatalogItemsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogFeatureCatalogItem", x => new { x.CatalogFeaturesId, x.CatalogItemsId });
                    table.ForeignKey(
                        name: "FK_CatalogFeatureCatalogItem_CatalogFeatures_CatalogFeaturesId",
                        column: x => x.CatalogFeaturesId,
                        principalTable: "CatalogFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogFeatureCatalogItem_CatalogItems_CatalogItemsId",
                        column: x => x.CatalogItemsId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItemVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProudctIdString = table.Column<string>(type: "text", nullable: true),
                    VariantId = table.Column<string>(type: "text", nullable: true),
                    VariantImageOrigin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VarianImageEnhanced = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VariantSKU = table.Column<string>(type: "text", nullable: true),
                    VariantKeyEN = table.Column<string>(type: "text", nullable: true),
                    VariantKeyDE = table.Column<string>(type: "text", nullable: true),
                    VariantLength = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantHeigt = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantWith = table.Column<decimal>(type: "numeric", nullable: false),
                    VariatVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantSellPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantStandart = table.Column<string>(type: "text", nullable: true),
                    CatalogItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogItemVariants_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnhancedImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Src = table.Column<string>(type: "text", nullable: true),
                    CatalogItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnhancedImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnhancedImages_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OriginalImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Src = table.Column<string>(type: "text", nullable: true),
                    CatalogItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OriginalImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginalImages_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogItemCatalogKit",
                columns: table => new
                {
                    CatalogItemsId = table.Column<int>(type: "integer", nullable: false),
                    CatalogKitsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItemCatalogKit", x => new { x.CatalogItemsId, x.CatalogKitsId });
                    table.ForeignKey(
                        name: "FK_CatalogItemCatalogKit_CatalogItems_CatalogItemsId",
                        column: x => x.CatalogItemsId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogItemCatalogKit_CatalogKits_CatalogKitsId",
                        column: x => x.CatalogKitsId,
                        principalTable: "CatalogKits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogFeatureCatalogItem_CatalogItemsId",
                table: "CatalogFeatureCatalogItem",
                column: "CatalogItemsId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemCatalogKit_CatalogKitsId",
                table: "CatalogItemCatalogKit",
                column: "CatalogKitsId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Name",
                table: "CatalogItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItemVariants_CatalogItemId",
                table: "CatalogItemVariants",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EnhancedImages_CatalogItemId",
                table: "EnhancedImages",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginalImages_CatalogItemId",
                table: "OriginalImages",
                column: "CatalogItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogFeatureCatalogItem");

            migrationBuilder.DropTable(
                name: "CatalogItemCatalogKit");

            migrationBuilder.DropTable(
                name: "CatalogItemVariants");

            migrationBuilder.DropTable(
                name: "EnhancedImages");

            migrationBuilder.DropTable(
                name: "IntegrationEventLog");

            migrationBuilder.DropTable(
                name: "OriginalImages");

            migrationBuilder.DropTable(
                name: "CatalogFeatures");

            migrationBuilder.DropTable(
                name: "CatalogKits");

            migrationBuilder.DropTable(
                name: "CatalogItems");
        }
    }
}
