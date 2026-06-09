using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Sales;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Sales;

namespace Materia.Application.Commands.Sales.FinalizeSale;

/// <summary>
/// Finalizes a consumer (walk-in / counter) sale in a single step: builds the sale from
/// the submitted items, applies discount/tax, records the serving staff, captures payment,
/// and decrements inventory. Inventory is allowed to go negative — stock deduction never
/// blocks the sale.
/// <para>
/// A partial payment (down payment / DP) leaves an outstanding balance recorded against the
/// sale (piutang / bon) and added to the registered customer's debt. The domain enforces
/// that credit sales require a registered customer.
/// </para>
/// </summary>
public sealed class FinalizeSaleCommandHandler(
    ISaleRepository           saleRepository,
    ICustomerRepository       customerRepository,
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

        sale.Finalize(command.ServedBy, command.Discount, command.TaxEnabled);

        // null AmountPaid ⇒ full payment of the (discount/tax-adjusted) grand total.
        var amountPaid = command.AmountPaid ?? sale.GrandTotal.Amount;
        sale.RecordSettlement(amountPaid, command.PaymentMethod, command.ServedBy);

        await DeductStockAsync(sale, command.ServedBy, ct);
        await saleRepository.SaveAsync(sale, ct);

        await UpdateCustomerDebtAsync(sale, command, ct);

        return new FinalizeSaleResult(
            sale.Id.Value,
            sale.ReferenceNo,
            sale.GrandTotal.Amount,
            sale.AmountPaid.Amount,
            sale.Payment?.Change.Amount ?? 0m,
            sale.OutstandingAmount.Amount,
            sale.OutstandingAmount.Amount > 0);
    }

    /// <summary>
    /// On a credit sale, adds the unpaid remainder to the registered customer's outstanding
    /// debt balance. The domain already guarantees an outstanding balance implies a customer.
    /// </summary>
    private async Task UpdateCustomerDebtAsync(
        Sale sale, FinalizeSaleCommand command, CancellationToken ct)
    {
        if (sale.OutstandingAmount.Amount <= 0 || command.CustomerId is null)
            return;

        var customer = await customerRepository.GetByIdAsync(
                           CustomerId.From(command.CustomerId.Value), ct)
                       ?? throw new DomainException("Pelanggan tidak ditemukan.");

        customer.IncurDebt(
            sale.OutstandingAmount.Amount, sale.Id.Value, sale.ReferenceNo, command.ServedBy);

        await customerRepository.SaveAsync(customer, ct);
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
