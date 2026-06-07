using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerVariantStockAndSaleColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReadModels_ProductId",
                table: "StockReadModels");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "StockReadModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "SaleItemReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "SaleItemReadModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReadModels_ProductId_VariantId",
                table: "StockReadModels",
                columns: new[] { "ProductId", "VariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReadModels_ProductId_VariantId",
                table: "StockReadModels");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StockReadModels");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "SaleItemReadModels");

            migrationBuilder.CreateIndex(
                name: "IX_StockReadModels_ProductId",
                table: "StockReadModels",
                column: "ProductId",
                unique: true);
        }
    }
}
