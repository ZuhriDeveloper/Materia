namespace Materia.Application.Contracts.Purchasing;

/// <summary>
/// Read-side query port for checking whether a product is referenced by any purchase order.
/// Used by the ChangeProductBaseUnit guard.
/// </summary>
public interface IPurchaseOrderProductQueryService
{
    /// <summary>Returns true if any purchase order line references the given product.</summary>
    Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default);
}
