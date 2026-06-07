namespace Materia.Application.Contracts.Sales;

public interface IStockDeductionService
{
    Task DeductAsync(
        Guid productId, Guid? variantId, decimal quantityInBaseUnit,
        string reason, string updatedBy,
        CancellationToken ct = default);
}
