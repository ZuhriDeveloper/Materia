using FluentAssertions;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Inventory;

namespace Materia.Tests.Inventory;

public class ProductExportRowMapperTests
{
    /// <summary>A product with complete master data: supplier, stock, harga beli, harga jual.</summary>
    private static ProductDto Complete(
        string name = "Semen Padang 50kg",
        string baseUnit = "sak",
        decimal salePrice = 15_000m,
        decimal stockQuantity = 10m,
        decimal? latestPurchasePrice = 12_000m,
        bool hasSupplier = true,
        bool isActive = true,
        string? barcode = "8991234567890",
        IReadOnlyList<CategorySummaryDto>? categories = null,
        DateTime? updatedAt = null,
        DateTime? createdAt = null)
        => new(
            Id: Guid.NewGuid(),
            Name: name,
            Description: null,
            BaseUnit: baseUnit,
            SalePrice: salePrice,
            Barcode: barcode,
            IsActive: isActive,
            CreatedBy: "user-1",
            CreatedAt: createdAt ?? new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedBy: null,
            UpdatedAt: updatedAt,
            UnitConversions: [],
            Categories: categories ?? [],
            ColorVariants: [],
            StockQuantity: stockQuantity,
            LatestPurchasePrice: latestPurchasePrice,
            HasSupplier: hasSupplier);

    [Fact]
    public void MapsScalarFields()
    {
        var p = Complete();

        var row = ProductExportRowMapper.ToRow(p);

        row.Name.Should().Be("Semen Padang 50kg");
        row.BaseUnit.Should().Be("sak");
        row.StockQuantity.Should().Be(10m);
        row.PurchasePrice.Should().Be(12_000m);
        row.SalePrice.Should().Be(15_000m);
        row.Barcode.Should().Be("8991234567890");
        row.IsActive.Should().BeTrue();
    }

    [Fact]
    public void JoinsCategoryNames()
    {
        var p = Complete(categories:
        [
            new CategorySummaryDto(Guid.NewGuid(), "Semen"),
            new CategorySummaryDto(Guid.NewGuid(), "Bahan Bangunan"),
        ]);

        ProductExportRowMapper.ToRow(p).Categories.Should().Be("Semen, Bahan Bangunan");
    }

    [Fact]
    public void UpdatedAt_FallsBackToCreatedAt_WhenNeverUpdated()
    {
        var created = new DateTime(2026, 2, 3, 9, 0, 0, DateTimeKind.Utc);
        var p = Complete(createdAt: created, updatedAt: null);

        ProductExportRowMapper.ToRow(p).UpdatedAt.Should().Be(created);
    }

    [Fact]
    public void UpdatedAt_UsesUpdatedAt_WhenPresent()
    {
        var updated = new DateTime(2026, 5, 6, 10, 0, 0, DateTimeKind.Utc);
        var p = Complete(updatedAt: updated);

        ProductExportRowMapper.ToRow(p).UpdatedAt.Should().Be(updated);
    }

    [Fact]
    public void CompleteProduct_HasEmptyNotes()
    {
        ProductExportRowMapper.ToRow(Complete()).Notes.Should().BeEmpty();
    }

    [Fact]
    public void MissingSupplier_NoteMentionsSupplier()
    {
        ProductExportRowMapper.ToRow(Complete(hasSupplier: false)).Notes
            .Should().Be("Belum ada: supplier");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NoStock_NoteMentionsStok(decimal stock)
    {
        ProductExportRowMapper.ToRow(Complete(stockQuantity: stock)).Notes
            .Should().Be("Belum ada: stok");
    }

    [Fact]
    public void MissingPurchasePrice_NoteMentionsHargaBeli()
    {
        ProductExportRowMapper.ToRow(Complete(latestPurchasePrice: null)).Notes
            .Should().Be("Belum ada: harga beli");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NoSalePrice_NoteMentionsHargaJual(decimal salePrice)
    {
        ProductExportRowMapper.ToRow(Complete(salePrice: salePrice)).Notes
            .Should().Be("Belum ada: harga jual");
    }

    [Fact]
    public void MultipleGaps_AreListedInOrder()
    {
        var p = Complete(
            hasSupplier: false,
            stockQuantity: 0m,
            latestPurchasePrice: null,
            salePrice: 0m);

        ProductExportRowMapper.ToRow(p).Notes
            .Should().Be("Belum ada: supplier, stok, harga beli, harga jual");
    }
}
