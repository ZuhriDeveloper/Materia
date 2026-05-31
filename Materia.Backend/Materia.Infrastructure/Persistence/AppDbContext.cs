using Materia.Infrastructure.Identity;
using Materia.Infrastructure.Persistence.EventStore;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    public DbSet<ProductReadModel> ProductReadModels => Set<ProductReadModel>();
    public DbSet<CategoryReadModel> CategoryReadModels => Set<CategoryReadModel>();
    public DbSet<UnitReadModel> UnitReadModels => Set<UnitReadModel>();
    public DbSet<StockReadModel> StockReadModels => Set<StockReadModel>();
    public DbSet<CustomerReadModel> CustomerReadModels => Set<CustomerReadModel>();
    public DbSet<CustomerAddressReadModel> CustomerAddressReadModels => Set<CustomerAddressReadModel>();
    public DbSet<SaleReadModel> SaleReadModels => Set<SaleReadModel>();
    public DbSet<SaleItemReadModel> SaleItemReadModels => Set<SaleItemReadModel>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StoredEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AggregateType, x.AggregateId, x.Version }).IsUnique();
            e.HasIndex(x => new { x.AggregateId, x.AggregateType });
        });

        builder.Entity<ProductReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsActive);
        });

        builder.Entity<CategoryReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<UnitReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<StockReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProductId).IsUnique();
        });

        builder.Entity<CustomerReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Phone).IsUnique();
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

        builder.Entity<SaleReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ReferenceNo).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.SaleType).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.PaymentMethod).HasConversion<string>();
            e.HasMany(x => x.Items)
             .WithOne()
             .HasForeignKey(i => i.SaleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SaleItemReadModel>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SaleId);
            e.HasIndex(x => x.ProductId);
        });
    }
}
