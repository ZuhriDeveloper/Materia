using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;

public sealed class ReceivePurchaseOrderCommandHandler(
    IPurchaseOrderRepository poRepository,
    IStockRepository stockRepository,
    ISupplierRepository supplierRepository)
{
    public async Task HandleAsync(ReceivePurchaseOrderCommand command, CancellationToken ct = default)
    {
        var po = await poRepository.GetByIdAsync(PurchaseOrderId.From(command.PurchaseOrderId), ct)
            ?? throw new DomainException($"Purchase order {command.PurchaseOrderId} not found.");

        var receipts = command.Lines
            .Select(l => (ProductId.From(l.ProductId), l.ReceivedQty))
            .ToList();

        po.Receive(receipts, command.ReceivedBy);
        await poRepository.SaveAsync(po, ct);

        await ReconcileStocksAsync(po, command, ct);

        if (po.UpdateCatalogOnReceipt)
            await SyncCatalogPricesAsync(po, command, ct);
    }

    private async Task ReconcileStocksAsync(
        PurchaseOrder po,
        ReceivePurchaseOrderCommand command,
        CancellationToken ct)
    {
        foreach (var input in command.Lines)
        {
            var productId = ProductId.From(input.ProductId);
            var line = po.Lines.First(l => l.ProductId == productId);

            // A product received for the first time may not have a stock record yet —
            // initialise it at zero, then reconcile the received quantity onto it.
            var stock = await stockRepository.GetAsync(productId, ct: ct)
                ?? Stock.Initialize(productId, line.Unit, command.ReceivedBy);

            stock.ReconcileFromPurchase(
                input.ReceivedQty, po.Id,
                line.UnitCost, line.Unit,
                command.ReceivedBy);

            await stockRepository.SaveAsync(stock, ct);
        }
    }

    /// <summary>
    /// Writes each received line's list price back to the supplier catalog so the next PO starts
    /// from the latest price. Best-effort: skipped for an inactive/missing supplier, and only
    /// written when the price actually differs from the current catalog price.
    /// </summary>
    private async Task SyncCatalogPricesAsync(
        PurchaseOrder po,
        ReceivePurchaseOrderCommand command,
        CancellationToken ct)
    {
        var supplier = await supplierRepository.GetByIdAsync(po.SupplierId, ct);
        if (supplier is null || !supplier.IsActive)
            return;

        foreach (var input in command.Lines)
        {
            var productId = ProductId.From(input.ProductId);
            var line = po.Lines.First(l => l.ProductId == productId);

            var latest = supplier.Catalog.TryGetValue(productId.Value, out var entry)
                ? entry.LatestPrice
                : null;

            if (latest is not null && latest.Amount == line.ListUnitCost)
                continue;   // no change — don't add a duplicate price-history entry

            supplier.SetPurchasePrice(
                productId,
                new PurchasePrice(line.ListUnitCost, latest?.Currency ?? "IDR", line.Unit, DateTime.UtcNow),
                command.ReceivedBy);
        }

        await supplierRepository.SaveAsync(supplier, ct);
    }
}
