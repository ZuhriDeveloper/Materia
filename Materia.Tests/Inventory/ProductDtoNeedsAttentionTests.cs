using FluentAssertions;
using Materia.Application.DTOs.Inventory;

namespace Materia.Tests.Inventory;

public class ProductDtoNeedsAttentionTests
{
    /// <summary>A product with complete master data: supplier, stock, harga beli, harga jual.</summary>
    private static ProductDto Complete(
        decimal salePrice = 15_000m,
        decimal stockQuantity = 10m,
        decimal? latestPurchasePrice = 12_000m,
        bool hasSupplier = true)
        => new(
            Id: Guid.NewGuid(),
            Name: "Semen Padang 50kg",
            Description: null,
            BaseUnit: "sak",
            SalePrice: salePrice,
            Barcode: null,
            IsActive: true,
            CreatedBy: "user-1",
            CreatedAt: DateTime.UtcNow,
            UpdatedBy: null,
            UpdatedAt: null,
            UnitConversions: [],
            Categories: [],
            ColorVariants: [],
            StockQuantity: stockQuantity,
            LatestPurchasePrice: latestPurchasePrice,
            HasSupplier: hasSupplier);

    [Fact]
    public void Complete_ProductDoesNotNeedAttention()
    {
        Complete().NeedsAttention.Should().BeFalse();
    }

    [Fact]
    public void MissingSupplier_NeedsAttention()
    {
        Complete(hasSupplier: false).NeedsAttention.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NoStock_NeedsAttention(decimal stock)
    {
        Complete(stockQuantity: stock).NeedsAttention.Should().BeTrue();
    }

    [Fact]
    public void MissingPurchasePrice_NeedsAttention()
    {
        Complete(latestPurchasePrice: null).NeedsAttention.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositivePurchasePrice_NeedsAttention(decimal purchasePrice)
    {
        Complete(latestPurchasePrice: purchasePrice).NeedsAttention.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NoSalePrice_NeedsAttention(decimal salePrice)
    {
        Complete(salePrice: salePrice).NeedsAttention.Should().BeTrue();
    }

    [Fact]
    public void DefaultEnrichment_NeedsAttention()
    {
        // When the read model is not enriched (defaults), the product reads as incomplete.
        var dto = Complete() with { StockQuantity = 0m, LatestPurchasePrice = null, HasSupplier = false };
        dto.NeedsAttention.Should().BeTrue();
    }
}
