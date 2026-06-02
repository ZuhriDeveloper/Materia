using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    ISupplierRepository supplierRepository,
    IPurchaseOrderRepository poRepository)
{
    public async Task<Guid> HandleAsync(CreatePurchaseOrderCommand command, CancellationToken ct = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(SupplierId.From(command.SupplierId), ct)
            ?? throw new DomainException($"Supplier {command.SupplierId} not found.");

        if (!supplier.IsActive)
            throw new DomainException("Cannot create PO for an inactive supplier.");

        var lines = BuildLines(supplier, command.Lines);
        var po = PurchaseOrder.Create(SupplierId.From(command.SupplierId), lines, command.CreatedBy);

        await poRepository.SaveAsync(po, ct);
        return po.Id.Value;
    }

    private static List<(ProductId, decimal, decimal, string)> BuildLines(
        Supplier supplier,
        IReadOnlyList<CreatePurchaseOrderLineInput> inputs)
    {
        var lines = new List<(ProductId, decimal, decimal, string)>(inputs.Count);

        foreach (var input in inputs)
        {
            if (!supplier.Catalog.TryGetValue(input.ProductId, out var entry))
                throw new DomainException(
                    $"Supplier has no catalog entry for product {input.ProductId}.");

            var latestPrice = entry.LatestPrice
                ?? throw new DomainException(
                    $"No purchase price defined for product {input.ProductId}.");

            lines.Add((ProductId.From(input.ProductId), input.Qty, latestPrice.Amount, latestPrice.Unit));
        }

        return lines;
    }
}
