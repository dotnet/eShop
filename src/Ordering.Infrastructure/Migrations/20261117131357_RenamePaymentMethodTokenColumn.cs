using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261117131357_RenamePaymentMethodTokenColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PaymentMethodId",
            schema: "ordering",
            table: "paymentmethods",
            newName: "PaymentToken");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PaymentToken",
            schema: "ordering",
            table: "paymentmethods",
            newName: "PaymentMethodId");
    }
}
