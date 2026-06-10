using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Materia.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Sale read model (CQRS query side).
/// <para>
/// Sale <b>domain events</b> are not stored in a Sale-specific table — they are appended
/// to the shared, append-only <c>StoredEvents</c> event store (see
/// <see cref="EventStore.StoredEvent"/>). This configuration therefore maps the projection
/// that the read side is built from.
/// </para>
/// </summary>
public sealed class SaleReadModelConfiguration : IEntityTypeConfiguration<SaleReadModel>
{
    public void Configure(EntityTypeBuilder<SaleReadModel> builder)
    {
        builder.ToTable("SaleReadModels");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.StoreId, x.ReferenceNo }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.CustomerId);

        builder.Property(x => x.SaleType).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.PaymentMethod).HasConversion<string>();

        builder.Property(x => x.IsDeliveryRequired).HasDefaultValue(false);
        builder.Property(x => x.ServedBy).HasMaxLength(100);

        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey(i => i.SaleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleItemReadModelConfiguration : IEntityTypeConfiguration<SaleItemReadModel>
{
    public void Configure(EntityTypeBuilder<SaleItemReadModel> builder)
    {
        builder.ToTable("SaleItemReadModels");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SaleId);
        builder.HasIndex(x => x.ProductId);
    }
}
