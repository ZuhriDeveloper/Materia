using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCustomerAddressCoordinatesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "CustomerAddressReadModels",
                type: "numeric(11,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "CustomerAddressReadModels",
                type: "numeric(11,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,8)");

            // Addresses saved before coordinates were optional defaulted to (0, 0) — never a
            // real Indonesian location. Treat those as "no pin" so they no longer display 0,0.
            migrationBuilder.Sql(
                @"UPDATE ""CustomerAddressReadModels"" SET ""Latitude"" = NULL, ""Longitude"" = NULL " +
                @"WHERE ""Latitude"" = 0 AND ""Longitude"" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "CustomerAddressReadModels",
                type: "numeric(11,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "CustomerAddressReadModels",
                type: "numeric(11,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,8)",
                oldNullable: true);
        }
    }
}
