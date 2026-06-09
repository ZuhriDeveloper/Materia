using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPettyCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PettyCashExpenseReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ReasonDetail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ReasonText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PettyCashExpenseReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_Category",
                table: "PettyCashExpenseReadModels",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_RecordedAt",
                table: "PettyCashExpenseReadModels",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_ReferenceNo",
                table: "PettyCashExpenseReadModels",
                column: "ReferenceNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PettyCashExpenseReadModels");
        }
    }
}
