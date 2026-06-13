using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderPaymentTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDueDate",
                table: "PurchaseOrderReadModels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTermUnit",
                table: "PurchaseOrderReadModels",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermValue",
                table: "PurchaseOrderReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderReadModels_PaymentDueDate",
                table: "PurchaseOrderReadModels",
                column: "PaymentDueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderReadModels_PaymentDueDate",
                table: "PurchaseOrderReadModels");

            migrationBuilder.DropColumn(
                name: "PaymentDueDate",
                table: "PurchaseOrderReadModels");

            migrationBuilder.DropColumn(
                name: "PaymentTermUnit",
                table: "PurchaseOrderReadModels");

            migrationBuilder.DropColumn(
                name: "PaymentTermValue",
                table: "PurchaseOrderReadModels");
        }
    }
}
