using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleReadModels",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNo       = table.Column<string>(type: "text", nullable: false),
                    CustomerId        = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName      = table.Column<string>(type: "text", nullable: false),
                    CustomerAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryAddress   = table.Column<string>(type: "text", nullable: true),
                    SaleType          = table.Column<string>(type: "text", nullable: false),
                    Status            = table.Column<string>(type: "text", nullable: false),
                    Subtotal          = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal        = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedBy         = table.Column<string>(type: "text", nullable: false),
                    CreatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAmount        = table.Column<decimal>(type: "numeric", nullable: true),
                    Change            = table.Column<decimal>(type: "numeric", nullable: true),
                    PaymentMethod     = table.Column<string>(type: "text", nullable: true),
                    PaidAt            = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_SaleReadModels", x => x.Id));

            migrationBuilder.CreateTable(
                name: "SaleItemReadModels",
                columns: table => new
                {
                    Id                 = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId             = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId          = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName        = table.Column<string>(type: "text", nullable: false),
                    UnitName           = table.Column<string>(type: "text", nullable: false),
                    Quantity           = table.Column<decimal>(type: "numeric", nullable: false),
                    QuantityInBaseUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice          = table.Column<decimal>(type: "numeric", nullable: false),
                    Subtotal           = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItemReadModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItemReadModels_SaleReadModels_SaleId",
                        column: x => x.SaleId,
                        principalTable: "SaleReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_ReferenceNo",
                table: "SaleReadModels",
                column: "ReferenceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_Status",
                table: "SaleReadModels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_CreatedAt",
                table: "SaleReadModels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_CustomerId",
                table: "SaleReadModels",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemReadModels_SaleId",
                table: "SaleItemReadModels",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItemReadModels_ProductId",
                table: "SaleItemReadModels",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SaleItemReadModels");
            migrationBuilder.DropTable(name: "SaleReadModels");
        }
    }
}
