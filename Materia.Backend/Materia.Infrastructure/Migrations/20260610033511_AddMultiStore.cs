using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Materia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitReadModels_Name",
                table: "UnitReadModels");

            migrationBuilder.DropIndex(
                name: "IX_StoredEvents_AggregateId_AggregateType",
                table: "StoredEvents");

            migrationBuilder.DropIndex(
                name: "IX_StoredEvents_AggregateType_AggregateId_Version",
                table: "StoredEvents");

            migrationBuilder.DropIndex(
                name: "IX_StockReadModels_ProductId_VariantId",
                table: "StockReadModels");

            migrationBuilder.DropIndex(
                name: "IX_SaleReadModels_ReferenceNo",
                table: "SaleReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ReceivablePaymentReadModels_IdempotencyKey",
                table: "ReceivablePaymentReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantReadModels_Barcode",
                table: "ProductVariantReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ProductReadModels_Barcode",
                table: "ProductReadModels");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashExpenseReadModels_ReferenceNo",
                table: "PettyCashExpenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReadModels_Phone",
                table: "CustomerReadModels");

            migrationBuilder.DropIndex(
                name: "IX_CategoryReadModels_Name",
                table: "CategoryReadModels");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "UnitReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "SupplierReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "StoredEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "StockReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "SaleReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "SaleItemReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ReceivablePaymentReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "PurchaseOrderReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "PurchaseInvoiceImages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ProductVariantReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ProductReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "PettyCashExpenseReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CustomerReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CustomerAddressReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CategoryReadModels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreReadModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreReadModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitReadModels_StoreId_Name",
                table: "UnitReadModels",
                columns: new[] { "StoreId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_StoreId_AggregateId_AggregateType",
                table: "StoredEvents",
                columns: new[] { "StoreId", "AggregateId", "AggregateType" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_StoreId_AggregateType_AggregateId_Version",
                table: "StoredEvents",
                columns: new[] { "StoreId", "AggregateType", "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReadModels_StoreId_ProductId_VariantId",
                table: "StockReadModels",
                columns: new[] { "StoreId", "ProductId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_StoreId_ReferenceNo",
                table: "SaleReadModels",
                columns: new[] { "StoreId", "ReferenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePaymentReadModels_StoreId_IdempotencyKey",
                table: "ReceivablePaymentReadModels",
                columns: new[] { "StoreId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantReadModels_StoreId_Barcode",
                table: "ProductVariantReadModels",
                columns: new[] { "StoreId", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReadModels_StoreId_Barcode",
                table: "ProductReadModels",
                columns: new[] { "StoreId", "Barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_StoreId_ReferenceNo",
                table: "PettyCashExpenseReadModels",
                columns: new[] { "StoreId", "ReferenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReadModels_StoreId_Phone",
                table: "CustomerReadModels",
                columns: new[] { "StoreId", "Phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReadModels_StoreId_Name",
                table: "CategoryReadModels",
                columns: new[] { "StoreId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreReadModels_Code",
                table: "StoreReadModels",
                column: "Code",
                unique: true);

            // ── Multi-store rollout: seed the default store, stamp the preserved catalog,
            //    and reset all transactional data. The default store id MUST match
            //    Materia.Infrastructure.Stores.StoreConstants.DefaultStoreId.
            //    Newly-added StoreId columns default to all-zeros; we stamp them here.

            // 1. Register the default store ("Toko 1") — read model + its self-owned event.
            migrationBuilder.Sql("""
                INSERT INTO "StoreReadModels" ("Id", "Name", "Code", "IsActive", "CreatedBy", "CreatedAt")
                SELECT '00000000-0000-0000-0000-000000000001', 'Toko 1', 'S1', TRUE,
                       'system@materia.local', TIMESTAMPTZ '2026-06-10 00:00:00+00'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "StoreReadModels" WHERE "Id" = '00000000-0000-0000-0000-000000000001');
                """);

            migrationBuilder.Sql("""
                INSERT INTO "StoredEvents" ("Id", "StoreId", "AggregateType", "AggregateId", "Version", "EventType", "EventData", "OccurredAt")
                SELECT 'a1111111-1111-1111-1111-111111111111',
                       '00000000-0000-0000-0000-000000000001',
                       'Store', '00000000-0000-0000-0000-000000000001', 1, 'StoreRegistered',
                       '{"storeId":"00000000-0000-0000-0000-000000000001","name":"Toko 1","code":"S1","registeredBy":"system@materia.local","occurredAt":"2026-06-10T00:00:00Z"}',
                       TIMESTAMPTZ '2026-06-10 00:00:00+00'
                WHERE NOT EXISTS (
                    SELECT 1 FROM "StoredEvents" WHERE "AggregateType" = 'Store'
                        AND "AggregateId" = '00000000-0000-0000-0000-000000000001');
                """);

            // 2. Preserve catalog masters (Category / Product / Unit) under the default store.
            migrationBuilder.Sql("""
                UPDATE "ProductReadModels"        SET "StoreId" = '00000000-0000-0000-0000-000000000001';
                UPDATE "ProductVariantReadModels" SET "StoreId" = '00000000-0000-0000-0000-000000000001';
                UPDATE "CategoryReadModels"       SET "StoreId" = '00000000-0000-0000-0000-000000000001';
                UPDATE "UnitReadModels"           SET "StoreId" = '00000000-0000-0000-0000-000000000001';
                UPDATE "StoredEvents"             SET "StoreId" = '00000000-0000-0000-0000-000000000001'
                    WHERE "AggregateType" IN ('Product', 'Category', 'Unit');
                UPDATE "AspNetUsers"              SET "StoreId" = '00000000-0000-0000-0000-000000000001'
                    WHERE "StoreId" IS NULL;
                """);

            // 3. Fresh start for all transactional data (event streams + projections).
            migrationBuilder.Sql("""
                DELETE FROM "StoredEvents"
                    WHERE "AggregateType" IN ('Sale', 'Customer', 'Stock', 'Supplier', 'PurchaseOrder', 'PettyCashExpense');
                DELETE FROM "SaleItemReadModels";
                DELETE FROM "SaleReadModels";
                DELETE FROM "ReceivablePaymentReadModels";
                DELETE FROM "CustomerAddressReadModels";
                DELETE FROM "CustomerReadModels";
                DELETE FROM "StockReadModels";
                DELETE FROM "SupplierReadModels";
                DELETE FROM "PurchaseInvoiceImages";
                DELETE FROM "PurchaseOrderReadModels";
                DELETE FROM "PettyCashExpenseReadModels";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the default store's self-owned events (the table itself is dropped below).
            migrationBuilder.Sql("""DELETE FROM "StoredEvents" WHERE "AggregateType" = 'Store';""");

            migrationBuilder.DropTable(
                name: "StoreReadModels");

            migrationBuilder.DropIndex(
                name: "IX_UnitReadModels_StoreId_Name",
                table: "UnitReadModels");

            migrationBuilder.DropIndex(
                name: "IX_StoredEvents_StoreId_AggregateId_AggregateType",
                table: "StoredEvents");

            migrationBuilder.DropIndex(
                name: "IX_StoredEvents_StoreId_AggregateType_AggregateId_Version",
                table: "StoredEvents");

            migrationBuilder.DropIndex(
                name: "IX_StockReadModels_StoreId_ProductId_VariantId",
                table: "StockReadModels");

            migrationBuilder.DropIndex(
                name: "IX_SaleReadModels_StoreId_ReferenceNo",
                table: "SaleReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ReceivablePaymentReadModels_StoreId_IdempotencyKey",
                table: "ReceivablePaymentReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantReadModels_StoreId_Barcode",
                table: "ProductVariantReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ProductReadModels_StoreId_Barcode",
                table: "ProductReadModels");

            migrationBuilder.DropIndex(
                name: "IX_PettyCashExpenseReadModels_StoreId_ReferenceNo",
                table: "PettyCashExpenseReadModels");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReadModels_StoreId_Phone",
                table: "CustomerReadModels");

            migrationBuilder.DropIndex(
                name: "IX_CategoryReadModels_StoreId_Name",
                table: "CategoryReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "UnitReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SupplierReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "StoredEvents");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "StockReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SaleReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SaleItemReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ReceivablePaymentReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "PurchaseOrderReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "PurchaseInvoiceImages");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ProductVariantReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ProductReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "PettyCashExpenseReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CustomerReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CustomerAddressReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CategoryReadModels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReadModels_Name",
                table: "UnitReadModels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_AggregateId_AggregateType",
                table: "StoredEvents",
                columns: new[] { "AggregateId", "AggregateType" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredEvents_AggregateType_AggregateId_Version",
                table: "StoredEvents",
                columns: new[] { "AggregateType", "AggregateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReadModels_ProductId_VariantId",
                table: "StockReadModels",
                columns: new[] { "ProductId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReadModels_ReferenceNo",
                table: "SaleReadModels",
                column: "ReferenceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivablePaymentReadModels_IdempotencyKey",
                table: "ReceivablePaymentReadModels",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantReadModels_Barcode",
                table: "ProductVariantReadModels",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReadModels_Barcode",
                table: "ProductReadModels",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PettyCashExpenseReadModels_ReferenceNo",
                table: "PettyCashExpenseReadModels",
                column: "ReferenceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReadModels_Phone",
                table: "CustomerReadModels",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReadModels_Name",
                table: "CategoryReadModels",
                column: "Name",
                unique: true);
        }
    }
}
