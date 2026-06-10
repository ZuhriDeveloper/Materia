using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "StoreReadModels",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDeliveryDistanceKm",
                table: "StoreReadModels",
                type: "numeric(10,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "StoreReadModels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreLogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLogos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreLogos_StoreId",
                table: "StoreLogos",
                column: "StoreId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreLogos");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "StoreReadModels");

            migrationBuilder.DropColumn(
                name: "MaxDeliveryDistanceKm",
                table: "StoreReadModels");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "StoreReadModels");
        }
    }
}
