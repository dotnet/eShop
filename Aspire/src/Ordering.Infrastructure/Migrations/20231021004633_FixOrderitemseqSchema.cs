using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderitemseqSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameSequence(
                name: "orderitemseq",
                newName: "orderitemseq",
                newSchema: "ordering");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "ordering",
                table: "orderstatus",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "ordering",
                table: "cardtypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameSequence(
                name: "orderitemseq",
                schema: "ordering",
                newName: "orderitemseq");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "ordering",
                table: "orderstatus",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "ordering",
                table: "cardtypes",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
