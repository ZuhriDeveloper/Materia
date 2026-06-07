using Materia.Application.Commands.Inventory.AdjustStock;
using Materia.Application.Contracts.Sales;

namespace Materia.Infrastructure.Sales;

public class StockDeductionService(AdjustStockCommandHandler handler) : IStockDeductionService
{
    public Task DeductAsync(
        Guid productId, Guid? variantId, decimal quantityInBaseUnit,
        string reason, string updatedBy,
        CancellationToken ct = default)
        => handler.HandleAsync(
            new AdjustStockCommand(productId, -quantityInBaseUnit, reason, updatedBy, variantId), ct);
}
