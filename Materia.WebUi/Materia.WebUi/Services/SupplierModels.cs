namespace Materia.WebUi.Services;

public record SupplierPriceDto(
    decimal  Amount,
    string   Currency,
    string   Unit,
    DateTime EffectiveFrom);

public record SupplierCatalogEntryDto(
    Guid                      ProductId,
    List<SupplierPriceDto>    Prices);

public record SupplierDto(
    Guid                          Id,
    string                        Name,
    string?                       ContactPhone,
    string?                       Description,
    string?                       SalesmanName,
    string?                       SalesmanPhone,
    bool                          IsActive,
    List<SupplierCatalogEntryDto> Catalog);
