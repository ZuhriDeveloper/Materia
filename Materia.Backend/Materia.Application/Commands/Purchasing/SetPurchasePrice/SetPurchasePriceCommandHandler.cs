using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.SetPurchasePrice;

public sealed class SetPurchasePriceCommandHandler(ISupplierRepository repository)
{
    public async Task HandleAsync(SetPurchasePriceCommand command, CancellationToken ct = default)
    {
        var supplier = await repository.GetByIdAsync(SupplierId.From(command.SupplierId), ct)
            ?? throw new DomainException($"Supplier {command.SupplierId} not found.");

        var price = new PurchasePrice(command.Amount, command.Currency, command.Unit, command.EffectiveFrom);
        supplier.SetPurchasePrice(ProductId.From(command.ProductId), price, command.UpdatedBy);

        await repository.SaveAsync(supplier, ct);
    }
}
