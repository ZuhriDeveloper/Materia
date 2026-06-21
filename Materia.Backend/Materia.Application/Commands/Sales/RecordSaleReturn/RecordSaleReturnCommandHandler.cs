using Materia.Application.Commands.Inventory.AdjustStock;
using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Sales;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Sales;

namespace Materia.Application.Commands.Sales.RecordSaleReturn;

public sealed class RecordSaleReturnCommandHandler(
    ISaleQueryRepository      saleQueryRepository,
    ISaleReturnRepository     saleReturnRepository,
    ICustomerRepository       customerRepository,
    AdjustStockCommandHandler adjustStockHandler)
{
    public async Task<RecordSaleReturnResult> HandleAsync(
        RecordSaleReturnCommand command, CancellationToken ct = default)
    {
        // 1. Load sale read model
        var sale = await saleQueryRepository.GetByIdAsync(command.OriginalSaleId, ct)
            ?? throw new DomainException("Penjualan tidak ditemukan.");

        // 2. Validate sale status — only Paid or PartiallyPaid sales can be returned
        if (sale.Status != SaleStatus.Paid && sale.Status != SaleStatus.PartiallyPaid)
            throw new DomainException(
                "Retur hanya dapat dilakukan pada penjualan yang sudah lunas atau lunas sebagian.");

        // 3. Resolve line metadata from sale items + validate qty
        var lineInputs = new List<SaleReturnLineInput>();
        foreach (var dto in command.Lines)
        {
            var saleItem = sale.Items.FirstOrDefault(i =>
                i.ProductId == dto.ProductId && i.VariantId == dto.VariantId)
                ?? throw new DomainException(
                    $"Produk '{dto.ProductId}' tidak ditemukan pada penjualan ini.");

            if (dto.Quantity > saleItem.Quantity)
                throw new DomainException(
                    $"Kuantitas retur ({dto.Quantity}) melebihi kuantitas yang dibeli " +
                    $"({saleItem.Quantity}) untuk produk '{saleItem.ProductName}'.");

            // Scale QuantityInBaseUnit proportionally to the returned qty
            var quantityInBaseUnit = saleItem.QuantityInBaseUnit / saleItem.Quantity * dto.Quantity;
            lineInputs.Add(new SaleReturnLineInput(
                dto.ProductId, saleItem.ProductName, dto.VariantId, saleItem.ColorName,
                dto.UnitName, dto.Quantity, quantityInBaseUnit, saleItem.UnitPrice));
        }

        // 4. Parse resolution
        if (!Enum.TryParse<ReturnResolution>(command.Resolution, out var resolution))
            throw new DomainException("Resolusi retur tidak valid.");

        if (resolution == ReturnResolution.DebtReduction && sale.OutstandingAmount <= 0)
            throw new DomainException("Penjualan ini tidak memiliki piutang yang dapat dikurangi.");

        // 5. Create SaleReturn aggregate
        var saleReturn = SaleReturn.Record(
            SaleId.From(command.OriginalSaleId),
            sale.ReferenceNo,
            lineInputs,
            resolution,
            command.Reason,
            command.ReturnedBy);

        // 6. Persist SaleReturn
        await saleReturnRepository.SaveAsync(saleReturn, ct);

        // 7. Restore stock — positive delta reverses the deduction made at sale time
        foreach (var line in saleReturn.Lines)
        {
            await adjustStockHandler.HandleAsync(
                new AdjustStockCommand(
                    line.ProductId,
                    line.QuantityInBaseUnit,
                    $"Retur penjualan {sale.ReferenceNo}",
                    command.ReturnedBy,
                    line.VariantId),
                ct);
        }

        // 8. Reduce customer outstanding debt when resolution is DebtReduction
        if (resolution == ReturnResolution.DebtReduction && sale.CustomerId.HasValue)
        {
            var customer = await customerRepository.GetByIdAsync(
                CustomerId.From(sale.CustomerId.Value), ct)
                ?? throw new DomainException("Pelanggan tidak ditemukan.");

            customer.ReduceDebtForReturn(
                saleReturn.TotalRefundAmount,
                saleReturn.Id.Value,
                command.ReturnedBy);

            await customerRepository.SaveAsync(customer, ct);
        }

        return new RecordSaleReturnResult(
            saleReturn.Id.Value,
            saleReturn.OriginalReferenceNo,
            saleReturn.TotalRefundAmount,
            saleReturn.Resolution.ToString());
    }
}
