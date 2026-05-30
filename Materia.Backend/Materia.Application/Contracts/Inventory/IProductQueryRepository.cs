using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Inventory;

public interface IProductQueryRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default);
}
