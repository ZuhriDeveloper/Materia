using Materia.Application.DTOs.Inventory;
using Materia.Application.DTOs.Sales;
using Materia.Domain.Sales;

namespace Materia.Application.Contracts.Sales;

public interface ISaleQueryRepository
{
    Task<SaleDto?>             GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<SaleDto>> GetPagedAsync(
        int page, int pageSize,
        SaleStatus? status,
        DateTime? from, DateTime? to,
        string? customerName = null, SaleType? saleType = null, string? referenceNo = null,
        CancellationToken ct = default);
}
