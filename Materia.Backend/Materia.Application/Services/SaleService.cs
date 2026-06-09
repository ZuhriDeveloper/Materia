using Materia.Application.Contracts.Customers;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Sales;
using Materia.Application.DTOs.Inventory;
using Materia.Application.DTOs.Sales;
using Materia.Domain.Common;
using Materia.Domain.Sales;

namespace Materia.Application.Services;

public class SaleService(
    ISaleRepository          saleRepository,
    ISaleQueryRepository     saleQueryRepository,
    ICustomerQueryRepository customerQueryRepository,
    IProductQueryRepository  productQueryRepository,
    IStockDeductionService   stockDeduction,
    IReferenceNumberGenerator referenceGenerator)
{
    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<Guid> CreateSaleAsync(
        CreateSaleRequest request, string createdBy, CancellationToken ct = default)
    {
        var refNo = await referenceGenerator.GenerateAsync(ct);
        var sale  = Sale.Create(refNo, createdBy);

        // Resolve customer
        var customerName = "Umum";
        if (request.CustomerId.HasValue)
        {
            var customer = await customerQueryRepository.GetByIdAsync(request.CustomerId.Value, ct)
                ?? throw new DomainException($"Pelanggan tidak ditemukan.");
            customerName = customer.Name;
        }
        sale.SetCustomer(request.CustomerId, customerName, createdBy);

        // Sale type
        if (string.Equals(request.SaleType, "Delivery", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.CustomerAddressId.HasValue || string.IsNullOrWhiteSpace(request.DeliveryAddress))
                throw new DomainException("Pengiriman memerlukan alamat tujuan.");
            sale.SetAsDelivery(request.CustomerAddressId.Value, request.DeliveryAddress, createdBy);
        }
        else
        {
            sale.SetAsPickup(createdBy);
        }

        await saleRepository.SaveAsync(sale, ct);
        return sale.Id.Value;
    }

    // ── Add Item ──────────────────────────────────────────────────────────────

    public async Task<Guid> AddItemAsync(
        Guid saleId, AddSaleItemRequest request,
        string updatedBy, CancellationToken ct = default)
    {
        var sale = await LoadAsync(saleId, ct);

        // Resolve conversion factor from entered unit → product base unit
        var product = await productQueryRepository.GetByIdAsync(request.ProductId, ct)
            ?? throw new DomainException($"Produk tidak ditemukan.");

        var quantityInBaseUnit = ResolveBaseQuantity(
            request.Quantity, request.UnitName, product);

        var itemId = sale.AddItem(
            request.ProductId,
            request.ProductName,
            request.UnitName,
            request.Quantity,
            quantityInBaseUnit,
            request.UnitPrice,
            updatedBy,
            request.VariantId,
            request.ColorName);

        await saleRepository.SaveAsync(sale, ct);
        return itemId.Value;
    }

    // ── Remove Item ───────────────────────────────────────────────────────────

    public async Task RemoveItemAsync(
        Guid saleId, Guid itemId,
        string updatedBy, CancellationToken ct = default)
    {
        var sale = await LoadAsync(saleId, ct);
        sale.RemoveItem(SaleItemId.From(itemId), updatedBy);
        await saleRepository.SaveAsync(sale, ct);
    }

    // ── Checkout ──────────────────────────────────────────────────────────────

    public async Task CheckoutAsync(
        Guid saleId, CheckoutRequest request,
        string confirmedBy, CancellationToken ct = default)
    {
        var sale = await LoadAsync(saleId, ct);

        sale.Confirm(confirmedBy);
        sale.Pay(request.PaidAmount, request.PaymentMethod, confirmedBy);

        // Deduct stock for every item (quantity already in base unit)
        foreach (var item in sale.Items)
        {
            await stockDeduction.DeductAsync(
                item.ProductId,
                item.VariantId,
                item.QuantityInBaseUnit,
                $"Penjualan {sale.ReferenceNo}",
                confirmedBy,
                ct);
        }

        await saleRepository.SaveAsync(sale, ct);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    public async Task CancelAsync(
        Guid saleId, string reason,
        string cancelledBy, CancellationToken ct = default)
    {
        var sale = await LoadAsync(saleId, ct);
        sale.Cancel(reason, cancelledBy);
        await saleRepository.SaveAsync(sale, ct);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => saleQueryRepository.GetByIdAsync(id, ct);

    public Task<PagedResult<SaleDto>> GetPagedAsync(
        int page, int pageSize,
        SaleStatus? status = null, DateTime? from = null, DateTime? to = null,
        string? customerName = null, SaleType? saleType = null, string? referenceNo = null,
        CancellationToken ct = default)
        => saleQueryRepository.GetPagedAsync(page, pageSize, status, from, to, customerName, saleType, referenceNo, ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Sale> LoadAsync(Guid saleId, CancellationToken ct)
        => await saleRepository.GetByIdAsync(SaleId.From(saleId), ct)
           ?? throw new DomainException($"Penjualan tidak ditemukan.");

    /// <summary>
    /// Converts <paramref name="quantity"/> in <paramref name="unitName"/> to the product's base unit.
    /// Conversions are stored as: FromUnit = BaseUnit, ToUnit = AltUnit, Factor = (1 BaseUnit = Factor AltUnit).
    /// Reverse: 1 AltUnit = 1/Factor BaseUnit.
    /// </summary>
    private static decimal ResolveBaseQuantity(
        decimal quantity, string unitName, ProductDto product)
    {
        if (unitName.Equals(product.BaseUnit, StringComparison.OrdinalIgnoreCase))
            return quantity;

        // Find conversion where the AltUnit matches the entered unit
        var conv = product.UnitConversions
            .FirstOrDefault(c => c.ToUnit.Equals(unitName, StringComparison.OrdinalIgnoreCase));

        if (conv is null)
            throw new DomainException(
                $"Unit '{unitName}' tidak tersedia untuk produk '{product.Name}'.");

        // 1 BaseUnit = Factor AltUnit  →  quantityInBase = quantity / Factor
        return quantity / conv.Factor;
    }
}
