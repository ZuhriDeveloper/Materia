using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivablePaymentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivablePaymentReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceivedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AllocationsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivablePaymentReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePaymentReadModels_CustomerId",
                table: "ReceivablePaymentReadModels",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePaymentReadModels_IdempotencyKey",
                table: "ReceivablePaymentReadModels",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivablePaymentReadModels");
        }
    }
}
