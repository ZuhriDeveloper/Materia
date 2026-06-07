-- =============================================================================
--  delete_dummy_products.sql
--  Removes the seeded DUMMY product catalog so the real catalog (imported from
--  "DAFTAR LIST BAHAN BANGUNAN") can be re-seeded by CatalogSeeder on next start.
--
--  Materia is EVENT-SOURCED: a "product" is not a single row. It is an append-only
--  event stream in "StoredEvents" (AggregateType = 'Product') plus the read-model
--  projections that are rebuilt from it. To remove a product cleanly we must delete
--  BOTH the event streams AND every projection derived from them.
--
--  What this script DELETES:
--    * all Product event streams           (StoredEvents where AggregateType='Product')
--    * all Stock event streams             (StoredEvents where AggregateType='Stock')
--    * product read models                 (ProductReadModels)
--    * product color-variant read models   (ProductVariantReadModels)
--    * stock read models                   (StockReadModels)        -- stock only exists per product
--
--  What this script KEEPS (the seeder is idempotent and reuses them by name):
--    * Categories  (CategoryReadModels + Category event streams)
--    * Units       (UnitReadModels    + Unit event streams)
--    * Customers, Suppliers, Sales, Purchase Orders, Identity users/roles
--
--  NOTE: If any dummy Sales / Purchase Orders reference the dummy products, their
--  stored product IDs will become dangling references. On a dummy database this is
--  expected. To also clear transactional dummy data, uncomment the OPTIONAL block.
--
--  Safe to run multiple times. Wrapped in a single transaction.
--  PostgreSQL — identifiers are quoted because EF Core created them PascalCase.
-- =============================================================================

BEGIN;

-- 1) Projections derived from product / stock events ---------------------------
DELETE FROM "StockReadModels";
DELETE FROM "ProductVariantReadModels";
DELETE FROM "ProductReadModels";

-- 2) Event streams (the source of truth) --------------------------------------
DELETE FROM "StoredEvents" WHERE "AggregateType" IN ('Product', 'Stock');

-- 3) OPTIONAL — also wipe dummy transactional data that pointed at the products.
--    Uncomment if your sales / purchasing tables only contain dummy records.
-- DELETE FROM "SaleItemReadModels";
-- DELETE FROM "SaleReadModels";
-- DELETE FROM "PurchaseOrderReadModels";
-- DELETE FROM "StoredEvents" WHERE "AggregateType" IN ('Sale', 'PurchaseOrder');

COMMIT;

-- After running this, restart the API (Materia.AppHost). DatabaseInitializer ->
-- CatalogSeeder will recreate the catalog from the real price-list data.
