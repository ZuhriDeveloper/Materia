using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Sales;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Sales;

namespace Materia.Application.Commands.Sales.FinalizeSale;

/// <summary>
/// Finalizes a consumer (walk-in / counter) sale in a single step: builds the sale from
/// the submitted items, records the serving staff, and decrements inventory. Inventory is
/// allowed to go negative — stock deduction never blocks the sale.
/// </summary>
public sealed class FinalizeSaleCommandHandler(
    ISaleRepository           saleRepository,
    ICustomerQueryRepository  customerQueryRepository,
    IProductQueryRepository   productQueryRepository,
    IStockDeductionService    stockDeduction,
    IReferenceNumberGenerator referenceGenerator)
{
    public async Task<FinalizeSaleResult> HandleAsync(
        FinalizeSaleCommand command, CancellationToken ct = default)
    {
        if (command.Items.Count == 0)
            throw new DomainException("Penjualan harus memiliki minimal satu item.");

        var referenceNo = await referenceGenerator.GenerateAsync(ct);
        var sale        = Sale.Create(referenceNo, command.ServedBy);

        await ApplyCustomerAsync(sale, command, ct);
        await AddItemsAsync(sale, command, ct);

        if (command.IsDeliveryRequired)
            sale.RequestDelivery(command.ServedBy);

        sale.Finalize(command.ServedBy);

        await DeductStockAsync(sale, command.ServedBy, ct);
        await saleRepository.SaveAsync(sale, ct);

        return new FinalizeSaleResult(sale.Id.Value, sale.ReferenceNo);
    }

    private async Task ApplyCustomerAsync(
        Sale sale, FinalizeSaleCommand command, CancellationToken ct)
    {
        if (command.CustomerId is null)
        {
            sale.SetCustomer(null, command.CustomerName ?? "Umum", command.ServedBy);
            return;
        }

        var customer = await customerQueryRepository.GetByIdAsync(command.CustomerId.Value, ct)
            ?? throw new DomainException("Pelanggan tidak ditemukan.");

        sale.SetCustomer(customer.Id, customer.Name, command.ServedBy);
    }

    private async Task AddItemsAsync(
        Sale sale, FinalizeSaleCommand command, CancellationToken ct)
    {
        foreach (var item in command.Items)
        {
            var product = await productQueryRepository.GetByIdAsync(item.ProductId, ct)
                ?? throw new DomainException($"Produk '{item.ProductId}' tidak ditemukan.");

            var quantityInBaseUnit = ResolveBaseQuantity(item.Quantity, item.UnitName, product);

            sale.AddItem(
                item.ProductId,
                item.ProductName,
                item.UnitName,
                item.Quantity,
                quantityInBaseUnit,
                item.UnitPrice,
                command.ServedBy,
                item.VariantId,
                item.ColorName);
        }
    }

    private async Task DeductStockAsync(Sale sale, string servedBy, CancellationToken ct)
    {
        foreach (var item in sale.Items)
        {
            await stockDeduction.DeductAsync(
                item.ProductId,
                item.VariantId,
                item.QuantityInBaseUnit,
                $"Penjualan {sale.ReferenceNo}",
                servedBy,
                ct);
        }
    }

    /// <summary>
    /// Converts <paramref name="quantity"/> in <paramref name="unitName"/> to the product's base unit.
    /// Conversions are stored as: FromUnit = BaseUnit, ToUnit = AltUnit, Factor = (1 BaseUnit = Factor AltUnit).
    /// </summary>
    private static decimal ResolveBaseQuantity(
        decimal quantity, string unitName, ProductDto product)
    {
        if (unitName.Equals(product.BaseUnit, StringComparison.OrdinalIgnoreCase))
            return quantity;

        var conversion = product.UnitConversions
            .FirstOrDefault(c => c.ToUnit.Equals(unitName, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                $"Unit '{unitName}' tidak tersedia untuk produk '{product.Name}'.");

        return quantity / conversion.Factor;
    }
}
