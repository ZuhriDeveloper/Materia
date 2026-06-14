using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IProductQueryRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> GetPagedAsync(
        int page, int pageSize, bool? isActive,
        string? search = null, Guid? categoryId = null,
        CancellationToken ct = default);
    /// <summary>All products matching the filters, unpaged and ordered by name — used by the Excel export.</summary>
    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        bool? isActive, string? search = null, Guid? categoryId = null,
        CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken ct = default);
}
