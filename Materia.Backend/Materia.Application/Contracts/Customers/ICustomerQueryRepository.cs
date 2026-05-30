using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Contracts.Customers;

public interface ICustomerQueryRepository
{
    Task<CustomerDto?>             GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> GetPagedAsync(
        int page, int pageSize, string? search, bool? isActive,
        CancellationToken ct = default);
    Task<IReadOnlyList<NearbyCustomerDto>> GetNearbyAsync(
        decimal latitude, decimal longitude, double radiusKm, int maxResults,
        CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeId = null,
        CancellationToken ct = default);
}
