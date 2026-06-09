using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditSalesAndCustomerDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingDebt",
                table: "CustomerReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "OutstandingDebt",
                table: "CustomerReadModels");
        }
    }
}
