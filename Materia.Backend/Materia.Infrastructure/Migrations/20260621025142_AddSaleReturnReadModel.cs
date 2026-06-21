using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReturnReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleReturnReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalSaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalReferenceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalRefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Resolution = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReturnedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnReadModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleReturnLineReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ColorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityInBaseUnit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnLineReadModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnLineReadModels_SaleReturnReadModels_SaleReturnId",
                        column: x => x.SaleReturnId,
                        principalTable: "SaleReturnReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLineReadModels_SaleReturnId",
                table: "SaleReturnLineReadModels",
                column: "SaleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnReadModels_OriginalSaleId",
                table: "SaleReturnReadModels",
                column: "OriginalSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnReadModels_ReturnedAt",
                table: "SaleReturnReadModels",
                column: "ReturnedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleReturnLineReadModels");

            migrationBuilder.DropTable(
                name: "SaleReturnReadModels");
        }
    }
}
