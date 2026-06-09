using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddressDistrictSubdistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "CustomerAddressReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subdistrict",
                table: "CustomerAddressReadModels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "CustomerAddressReadModels");

            migrationBuilder.DropColumn(
                name: "Subdistrict",
                table: "CustomerAddressReadModels");
        }
    }
}
