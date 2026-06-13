using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.RecordPurchaseReturn;

/// <summary>
/// Records a return of received goods to the supplier (e.g. broken on arrival): reduces the
/// PO's net received quantity — and therefore the amount owed — and lowers stock to match.
/// Mirrors <c>ReceivePurchaseOrderCommandHandler</c> in reverse.
/// </summary>
public sealed class RecordPurchaseReturnCommandHandler(
    IPurchaseOrderRepository poRepository,
    IStockRepository stockRepository)
{
    public async Task HandleAsync(RecordPurchaseReturnCommand command, CancellationToken ct = default)
    {
        var po = await poRepository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
            ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} not found.");

        var returns = command.Lines
            .Select(l => (ProductId.From(l.ProductId), l.ReturnedQty))
            .ToList();

        po.RecordReturn(returns, command.Reason, command.ReturnedBy);
        await poRepository.SaveAsync(po, ct);

        await ReduceStocksAsync(po, command, ct);
    }

    private async Task ReduceStocksAsync(
        PurchaseOrder po,
        RecordPurchaseReturnCommand command,
        CancellationToken ct)
    {
        foreach (var input in command.Lines)
        {
            var productId = ProductId.From(input.ProductId);
            var line = po.Lines.First(l => l.ProductId == productId);

            var stock = await stockRepository.GetAsync(productId, ct: ct)
                ?? Stock.Initialize(productId, line.Unit, command.ReturnedBy);

            stock.ReduceFromPurchaseReturn(
                input.ReturnedQty, po.Id,
                line.UnitCost, line.Unit,
                command.ReturnedBy);

            await stockRepository.SaveAsync(stock, ct);
        }
    }
}
