using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

/// <summary>
/// Maps an enriched <see cref="ProductDto"/> to a flat <see cref="ProductExportRow"/> for
/// the Excel export, including the "Catatan" note that lists the same master-data gaps the
/// admin product list flags via <see cref="ProductDto.NeedsAttention"/>.
/// </summary>
public static class ProductExportRowMapper
{
    public static ProductExportRow ToRow(ProductDto p) => new(
        Name: p.Name,
        BaseUnit: p.BaseUnit,
        StockQuantity: p.StockQuantity,
        PurchasePrice: p.LatestPurchasePrice,
        SalePrice: p.SalePrice,
        Barcode: p.Barcode,
        Categories: string.Join(", ", p.Categories.Select(c => c.Name)),
        IsActive: p.IsActive,
        UpdatedAt: p.UpdatedAt ?? p.CreatedAt,
        Notes: BuildNotes(p));

    /// <summary>
    /// Lists the master data a product is still missing — supplier, stock, harga beli,
    /// harga jual — mirroring <see cref="ProductDto.NeedsAttention"/>. Returns an empty
    /// string when the product is complete.
    /// </summary>
    public static string BuildNotes(ProductDto p)
    {
        var missing = new List<string>();
        if (!p.HasSupplier) missing.Add("supplier");
        if (p.StockQuantity <= 0m) missing.Add("stok");
        if (p.LatestPurchasePrice is null or <= 0m) missing.Add("harga beli");
        if (p.SalePrice <= 0m) missing.Add("harga jual");

        return missing.Count == 0 ? "" : "Belum ada: " + string.Join(", ", missing);
    }
}
