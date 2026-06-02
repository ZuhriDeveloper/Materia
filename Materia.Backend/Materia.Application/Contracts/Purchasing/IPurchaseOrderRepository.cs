using Materia.Domain.Purchasing;

namespace Materia.Application.Contracts.Purchasing;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken ct = default);
    Task SaveAsync(PurchaseOrder po, CancellationToken ct = default);
}
