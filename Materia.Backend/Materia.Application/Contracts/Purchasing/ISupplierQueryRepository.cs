namespace Materia.Application.Contracts.Purchasing;

public interface ISupplierQueryRepository
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(bool activeOnly, CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(Guid supplierId, CancellationToken ct = default);
    Task<SupplierBestPriceResult?> GetBestPriceForProductAsync(Guid productId, CancellationToken ct = default);
}

public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactPhone,
    string? Description,
    string? SalesmanName,
    string? SalesmanPhone,
    bool IsActive,
    IReadOnlyList<SupplierCatalogEntryDto> Catalog);

public record SupplierCatalogEntryDto(
    Guid ProductId,
    IReadOnlyList<SupplierPriceDto> Prices);

public record SupplierPriceDto(
    decimal Amount,
    string Currency,
    string Unit,
    DateTime EffectiveFrom);

public record SupplierBestPriceResult(Guid SupplierId, string SupplierName, decimal UnitCost, string Unit);
