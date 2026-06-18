namespace Materia.Application.Contracts.Sales;

/// <summary>
/// Read-side query port for checking whether a product appears in any sale.
/// Used by the ChangeProductBaseUnit guard.
/// </summary>
public interface ISaleProductQueryService
{
    /// <summary>Returns true if any sale line item references the given product.</summary>
    Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default);
}
