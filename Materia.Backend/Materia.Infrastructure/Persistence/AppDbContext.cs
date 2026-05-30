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
    }
}
