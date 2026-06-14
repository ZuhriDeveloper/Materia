using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierDescriptionAndSalesman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SupplierReadModels",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesmanName",
                table: "SupplierReadModels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesmanPhone",
                table: "SupplierReadModels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SupplierReadModels");

            migrationBuilder.DropColumn(
                name: "SalesmanName",
                table: "SupplierReadModels");

            migrationBuilder.DropColumn(
                name: "SalesmanPhone",
                table: "SupplierReadModels");
        }
    }
}
