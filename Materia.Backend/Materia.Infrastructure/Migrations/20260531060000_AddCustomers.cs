using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerReadModels",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uuid", nullable: false),
                    Name      = table.Column<string>(type: "text", nullable: false),
                    Phone     = table.Column<string>(type: "text", nullable: false),
                    Email     = table.Column<string>(type: "text", nullable: true),
                    IsActive  = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_CustomerReadModels", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CustomerAddressReadModels",
                columns: table => new
                {
                    Id         = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label      = table.Column<string>(type: "text", nullable: false),
                    Street     = table.Column<string>(type: "text", nullable: false),
                    City       = table.Column<string>(type: "text", nullable: false),
                    Province   = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Latitude   = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    Longitude  = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    IsDefault  = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddressReadModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddressReadModels_CustomerReadModels_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReadModels_Phone",
                table: "CustomerReadModels",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReadModels_Name",
                table: "CustomerReadModels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReadModels_IsActive",
                table: "CustomerReadModels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddressReadModels_CustomerId",
                table: "CustomerAddressReadModels",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddressReadModels_Latitude_Longitude",
                table: "CustomerAddressReadModels",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CustomerAddressReadModels");
            migrationBuilder.DropTable(name: "CustomerReadModels");
        }
    }
}
