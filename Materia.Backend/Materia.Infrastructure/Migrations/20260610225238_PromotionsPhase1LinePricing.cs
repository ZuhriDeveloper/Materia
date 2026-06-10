using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PromotionsPhase1LinePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrossSubtotal",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscountTotal",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "SaleReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPerUnit",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossSubtotal",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineCost",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineDiscount",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ListUnitPrice",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetUnitPrice",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SaleItemReadModels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "PettyCashExpenseReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "ChangeFundDepositReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ChangeFundWithdrawalReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeFundWithdrawalReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_StoreId_IdempotencyKey",
                table: "PettyCashExpenseReadModels",
                columns: new[] { "StoreId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFundDepositReadModels_StoreId_IdempotencyKey",
                table: "ChangeFundDepositReadModels",
                columns: new[] { "StoreId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFundWithdrawalReadModels_RecordedAt",
                table: "ChangeFundWithdrawalReadModels",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFundWithdrawalReadModels_StoreId",
                table: "ChangeFundWithdrawalReadModels",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFundWithdrawalReadModels_StoreId_IdempotencyKey",
                table: "ChangeFundWithdrawalReadModels",
                columns: new[] { "StoreId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeFundWithdrawalReadModels");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashExpenseReadModels_StoreId_IdempotencyKey",
                table: "PettyCashExpenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ChangeFundDepositReadModels_StoreId_IdempotencyKey",
                table: "ChangeFundDepositReadModels");

            migrationBuilder.DropColumn(
                name: "GrossSubtotal",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "LineDiscountTotal",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "DiscountPerUnit",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "GrossSubtotal",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "LineCost",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "LineDiscount",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "ListUnitPrice",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "NetUnitPrice",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "PettyCashExpenseReadModels");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ChangeFundDepositReadModels");
        }
    }
}
