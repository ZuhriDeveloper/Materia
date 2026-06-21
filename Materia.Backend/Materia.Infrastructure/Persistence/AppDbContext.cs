using Materia.Application.Contracts.Stores;
using Materia.Infrastructure.Identity;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentStore currentStore)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    /// The store the current operation is scoped to, used by the per-store global query
    /// filters. <c>null</c> for a platform SuperAdmin or unscoped context, in which case
    /// the filters are no-ops (all rows visible). Referenced directly in the filter
    /// lambdas so EF Core re-evaluates it per query rather than caching a value.
    /// </summary>
    public Guid? CurrentStoreId => currentStore.StoreIdOrNull;

    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    public DbSet<StoreReadModel> StoreReadModels => Set<StoreReadModel>();
    public DbSet<ProductReadModel> ProductReadModels => Set<ProductReadModel>();
    public DbSet<ProductVariantReadModel> ProductVariantReadModels => Set<ProductVariantReadModel>();
    public DbSet<CategoryReadModel> CategoryReadModels => Set<CategoryReadModel>();
    public DbSet<UnitReadModel> UnitReadModels => Set<UnitReadModel>();
    public DbSet<StockReadModel> StockReadModels => Set<StockReadModel>();
    public DbSet<CustomerReadModel> CustomerReadModels => Set<CustomerReadModel>();
    public DbSet<CustomerAddressReadModel> CustomerAddressReadModels => Set<CustomerAddressReadModel>();
    public DbSet<ReceivablePaymentReadModel> ReceivablePaymentReadModels => Set<ReceivablePaymentReadModel>();
    public DbSet<SaleReadModel> SaleReadModels => Set<SaleReadModel>();
    public DbSet<SaleItemReadModel> SaleItemReadModels => Set<SaleItemReadModel>();
    public DbSet<SaleReturnReadModel> SaleReturnReadModels => Set<SaleReturnReadModel>();
    public DbSet<SaleReturnLineReadModel> SaleReturnLineReadModels => Set<SaleReturnLineReadModel>();
    public DbSet<SupplierReadModel> SupplierReadModels => Set<SupplierReadModel>();
    public DbSet<PurchaseOrderReadModel> PurchaseOrderReadModels => Set<PurchaseOrderReadModel>();
    public DbSet<PurchaseInvoiceImage> PurchaseInvoiceImages => Set<PurchaseInvoiceImage>();
    public DbSet<StoreLogo> StoreLogos => Set<StoreLogo>();
    public DbSet<PettyCashExpenseReadModel> PettyCashExpenseReadModels => Set<PettyCashExpenseReadModel>();
    public DbSet<ChangeFundDepositReadModel> ChangeFundDepositReadModels => Set<ChangeFundDepositReadModel>();
    public DbSet<ChangeFundWithdrawalReadModel> ChangeFundWithdrawalReadModels => Set<ChangeFundWithdrawalReadModel>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Event store ─────────────────────────────────────────────────────────
        // NOT store-filtered: the event store is queried directly, and repositories add
        // an explicit StoreId predicate on load as defense-in-depth.
        builder.Entity<StoredEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreId, x.AggregateType, x.AggregateId, x.Version }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.AggregateId, x.AggregateType });
        });

        // ── Store registry (platform-level — intentionally NOT store-filtered) ───
        builder.Entity<StoreReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            // Optional store parameters
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.MaxDeliveryDistanceKm).HasColumnType("decimal(10,3)");
        });

        // ── Store logos (plain attachment table — NOT event-sourced) ────────────
        builder.Entity<StoreLogo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Content).HasColumnType("bytea").IsRequired();
            e.Property(x => x.UploadedBy).HasMaxLength(100).IsRequired();
            // Unique: one logo per store (enforced by DB, not just the replace-on-upload logic)
            e.HasIndex(x => x.StoreId).IsUnique();
        });

        builder.Entity<ProductReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsActive);
            e.Property(x => x.Barcode).HasMaxLength(100);
            // Unique per barcode WITHIN a store; PostgreSQL treats NULLs as distinct, so
            // products without a barcode are unaffected.
            e.HasIndex(x => new { x.StoreId, x.Barcode }).IsUnique();
        });

        builder.Entity<ProductVariantReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId);
            e.Property(x => x.ColorName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ColorCode).HasMaxLength(50);
            e.Property(x => x.Barcode).HasMaxLength(100);
            // Unique per barcode WITHIN a store; PostgreSQL treats NULLs as distinct.
            e.HasIndex(x => new { x.StoreId, x.Barcode }).IsUnique();
        });

        builder.Entity<CategoryReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreId, x.Name }).IsUnique();
        });

        builder.Entity<UnitReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreId, x.Name }).IsUnique();
        });

        builder.Entity<StockReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            // One stock row per (product, variant) WITHIN a store. Product-level stock has a null VariantId.
            e.HasIndex(x => new { x.StoreId, x.ProductId, x.VariantId }).IsUnique();
        });

        builder.Entity<CustomerReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreId, x.Phone }).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsActive);
            e.HasMany(x => x.Addresses)
             .WithOne()
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustomerAddressReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => new { x.Latitude, x.Longitude });
            e.Property(x => x.Latitude).HasColumnType("decimal(11,8)");
            e.Property(x => x.Longitude).HasColumnType("decimal(11,8)");
        });

        builder.Entity<ReceivablePaymentReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            // Idempotency: at most one payment per client key WITHIN a store. PostgreSQL
            // enforces this even under a concurrent race, so a duplicate submission can
            // never over-collect.
            e.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NewBalance).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReceivedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.AllocationsJson).HasColumnType("jsonb").IsRequired();
        });

        builder.ApplyConfiguration(new Configurations.SaleReadModelConfiguration());
        builder.ApplyConfiguration(new Configurations.SaleItemReadModelConfiguration());

        builder.Entity<SaleReturnReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OriginalSaleId);
            e.HasIndex(x => x.ReturnedAt);
            e.Property(x => x.OriginalReferenceNo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.ReturnedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.TotalRefundAmount).HasColumnType("decimal(18,2)");
            e.HasMany(x => x.Lines)
             .WithOne()
             .HasForeignKey(l => l.SaleReturnId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SaleReturnLineReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SaleReturnId);
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ColorName).HasMaxLength(100);
            e.Property(x => x.UnitName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Quantity).HasColumnType("decimal(18,4)");
            e.Property(x => x.QuantityInBaseUnit).HasColumnType("decimal(18,4)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        });

        builder.Entity<SupplierReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.SalesmanName).HasMaxLength(200);
            e.Property(x => x.SalesmanPhone).HasMaxLength(50);
            e.Property(x => x.CatalogJson).HasColumnType("jsonb");
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsActive);
        });

        builder.Entity<PurchaseOrderReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.LinesJson).HasColumnType("jsonb");
            e.Property(x => x.PaymentTermUnit).HasMaxLength(20);
            e.HasIndex(x => x.SupplierId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.PaymentDueDate);
        });

        builder.Entity<PurchaseInvoiceImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Content).HasColumnType("bytea").IsRequired();
            e.Property(x => x.UploadedBy).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.PurchaseOrderId);
        });

        builder.Entity<PettyCashExpenseReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
            e.Property(x => x.ReasonDetail).HasMaxLength(300);
            e.Property(x => x.ReasonText).HasMaxLength(300).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.ReferenceNo).HasMaxLength(30).IsRequired();
            e.Property(x => x.RecordedBy).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.RecordedAt);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => new { x.StoreId, x.ReferenceNo }).IsUnique();
            // Idempotency: at most one expense per client key WITHIN a store. PostgreSQL
            // enforces this even under a concurrent race, so a duplicate submission can
            // never record the expense (or its TukarUangKembalian deposit) twice.
            e.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
        });

        builder.Entity<ChangeFundDepositReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.SourceReferenceNo).HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.RecordedBy).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.RecordedAt);
            e.HasIndex(x => x.StoreId);
            // Idempotency: at most one deposit per client key WITHIN a store — the ledger
            // is append-only, so a duplicate would permanently inflate the balance.
            e.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
        });

        builder.Entity<ChangeFundWithdrawalReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            e.Property(x => x.RecordedBy).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.RecordedAt);
            e.HasIndex(x => x.StoreId);
            e.HasIndex(x => new { x.StoreId, x.IdempotencyKey }).IsUnique();
        });

        // ── Per-store global query filters (tenant isolation) ────────────────────
        // Null-tolerant: a null CurrentStoreId (SuperAdmin / seed) is a no-op so all rows
        // are visible; otherwise rows are scoped to the current store. Applied to every
        // read model — including child tables, so Include() and direct queries stay
        // consistent. The Stores registry, event store and Identity tables are excluded.
        builder.Entity<ProductReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<ProductVariantReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<CategoryReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<UnitReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<StockReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<CustomerReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<CustomerAddressReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<ReceivablePaymentReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<SaleReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<SaleItemReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<SaleReturnReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<SaleReturnLineReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<SupplierReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<PurchaseOrderReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<PurchaseInvoiceImage>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<PettyCashExpenseReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<ChangeFundDepositReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
        builder.Entity<ChangeFundWithdrawalReadModel>().HasQueryFilter(x => CurrentStoreId == null || x.StoreId == CurrentStoreId);
    }
}
