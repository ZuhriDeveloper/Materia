namespace Materia.Application.Queries.Inventory;

/// <summary>
/// A flat, presentation-ready projection of a product for the Excel export. Numeric and
/// date values stay typed so the exporter can apply spreadsheet number/date formats;
/// <see cref="Notes"/> carries the human-readable list of master-data gaps that need
/// attention (empty when the product is complete).
/// </summary>
public record ProductExportRow(
    string Name,
    string BaseUnit,
    decimal StockQuantity,
    decimal? PurchasePrice,
    decimal SalePrice,
    string? Barcode,
    string Categories,
    bool IsActive,
    DateTime UpdatedAt,
    string Notes);
