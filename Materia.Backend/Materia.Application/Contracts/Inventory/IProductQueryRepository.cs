using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IProductQueryRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken ct = default);
}
