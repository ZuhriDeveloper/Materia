using Materia.Domain.Common;
using Materia.Domain.Sales.Events;

namespace Materia.Domain.Sales;

public sealed class Sale : AggregateRoot<SaleId>
{
    private readonly List<SaleItem> _items = [];

    public string       ReferenceNo       { get; private set; } = default!;
    public Guid?        CustomerId        { get; private set; }
    public string       CustomerName      { get; private set; } = "Umum";
    public Guid?        CustomerAddressId { get; private set; }
    public string?      DeliveryAddress   { get; private set; }
    public SaleType     SaleType          { get; private set; }
    public SaleStatus   Status            { get; private set; }
    public SalePayment? Payment           { get; private set; }
    public string       CreatedBy         { get; private set; } = default!;
    public string?      ServedBy          { get; private set; }
    public bool         IsDeliveryRequired { get; private set; }

    public IReadOnlyList<SaleItem> Items => _items.AsReadOnly();

    public Money Subtotal   => new(_items.Sum(i => i.Subtotal.Amount));
    public Money GrandTotal => Subtotal;

    private Sale() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Sale Create(string referenceNo, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(referenceNo))
            throw new DomainException("Nomor referensi tidak boleh kosong.");

        var sale = new Sale();
        sale.Raise(new SaleCreated(SaleId.New(), referenceNo.Trim(), createdBy, DateTime.UtcNow));
        return sale;
    }

    public static Sale Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var sale = new Sale();
        sale.Load(events);
        return sale;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void SetCustomer(Guid? customerId, string customerName, string updatedBy)
    {
        EnsureDraft();
        Raise(new SaleCustomerSet(
            Id, customerId,
            string.IsNullOrWhiteSpace(customerName) ? "Umum" : customerName.Trim(),
            updatedBy, DateTime.UtcNow));
    }

    public void SetAsPickup(string updatedBy)
    {
        EnsureDraft();
        Raise(new SaleTypeSetToPickup(Id, updatedBy, DateTime.UtcNow));
    }

    public void SetAsDelivery(Guid customerAddressId, string deliveryAddress, string updatedBy)
    {
        EnsureDraft();
        if (CustomerId is null)
            throw new DomainException("Pengiriman hanya tersedia untuk pelanggan terdaftar.");
        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new DomainException("Alamat pengiriman tidak boleh kosong.");

        Raise(new SaleTypeSetToDelivery(
            Id, customerAddressId, deliveryAddress.Trim(), updatedBy, DateTime.UtcNow));
    }

    public SaleItemId AddItem(
        Guid    productId,
        string  productName,
        string  unitName,
        decimal quantity,
        decimal quantityInBaseUnit,
        decimal unitPrice,
        string  updatedBy,
        Guid?   variantId = null,
        string? colorName = null)
    {
        EnsureDraft();
        if (quantity <= 0)
            throw new DomainException("Kuantitas harus lebih dari nol.");
        if (unitPrice < 0)
            throw new DomainException("Harga tidak boleh negatif.");

        var itemId = SaleItemId.New();
        Raise(new SaleItemAdded(
            Id, itemId,
            productId, productName.Trim(),
            unitName.Trim(),
            quantity, quantityInBaseUnit, unitPrice,
            updatedBy, DateTime.UtcNow,
            variantId,
            string.IsNullOrWhiteSpace(colorName) ? null : colorName.Trim()));

        return itemId;
    }

    public void RemoveItem(SaleItemId itemId, string updatedBy)
    {
        EnsureDraft();
        if (_items.All(i => i.Id != itemId))
            throw new DomainException($"Item '{itemId}' tidak ditemukan.");

        Raise(new SaleItemRemoved(Id, itemId, updatedBy, DateTime.UtcNow));
    }

    public void Confirm(string confirmedBy)
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new DomainException("Tidak dapat mengkonfirmasi penjualan tanpa item.");
        if (SaleType == SaleType.Delivery && CustomerAddressId is null)
            throw new DomainException("Penjualan pengiriman harus memiliki alamat tujuan.");

        Raise(new SaleConfirmed(Id, GrandTotal.Amount, confirmedBy, DateTime.UtcNow));
    }

    /// <summary>Flags this consumer sale as needing delivery (lightweight, address-optional).</summary>
    public void RequestDelivery(string requestedBy)
    {
        EnsureDraft();
        if (IsDeliveryRequired) return;  // idempotent

        Raise(new DeliveryRequested(Id, requestedBy, DateTime.UtcNow));
    }

    /// <summary>
    /// Finalizes a consumer sale in a single step: locks the sale, records the serving
    /// staff, and emits the event the application layer uses to decrement inventory.
    /// </summary>
    public void Finalize(string servedBy)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(servedBy))
            throw new DomainException("Staf yang melayani (ServedBy) tidak boleh kosong.");
        if (_items.Count == 0)
            throw new DomainException("Tidak dapat menyelesaikan penjualan tanpa item.");

        Raise(new SaleFinalized(
            Id, GrandTotal.Amount, servedBy.Trim(), IsDeliveryRequired, DateTime.UtcNow));
    }

    public void Pay(decimal paidAmount, PaymentMethod method, string paidBy)
    {
        if (Status == SaleStatus.Paid) return;  // idempotent

        if (Status != SaleStatus.Confirmed)
            throw new DomainException("Penjualan harus dikonfirmasi sebelum pembayaran.");

        // Validates paidAmount >= grandTotal internally
        var payment = new SalePayment(new Money(paidAmount), GrandTotal, method, DateTime.UtcNow);

        Raise(new SalePaid(
            Id,
            payment.PaidAmount.Amount,
            payment.Change.Amount,
            method,
            payment.PaidAt,
            DateTime.UtcNow));
    }

    public void Cancel(string reason, string cancelledBy)
    {
        if (Status == SaleStatus.Cancelled) return;
        if (Status == SaleStatus.Paid)
            throw new DomainException("Penjualan yang sudah dibayar tidak dapat dibatalkan.");

        Raise(new SaleCancelled(Id, reason, cancelledBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case SaleCreated e:
                Id          = e.SaleId;
                ReferenceNo = e.ReferenceNo;
                CreatedBy   = e.CreatedBy;
                Status      = SaleStatus.Draft;
                SaleType    = SaleType.Pickup;
                break;

            case SaleCustomerSet e:
                CustomerId   = e.CustomerId;
                CustomerName = e.CustomerName;
                break;

            case SaleTypeSetToPickup:
                SaleType          = SaleType.Pickup;
                CustomerAddressId = null;
                DeliveryAddress   = null;
                break;

            case SaleTypeSetToDelivery e:
                SaleType          = SaleType.Delivery;
                CustomerAddressId = e.CustomerAddressId;
                DeliveryAddress   = e.DeliveryAddress;
                break;

            case SaleItemAdded e:
                _items.Add(new SaleItem(
                    e.ItemId, e.ProductId, e.ProductName,
                    e.UnitName, e.Quantity, e.QuantityInBaseUnit,
                    new Money(e.UnitPrice), e.VariantId, e.ColorName));
                break;

            case SaleItemRemoved e:
                _items.RemoveAll(i => i.Id == e.ItemId);
                break;

            case SaleConfirmed:
                Status = SaleStatus.Confirmed;
                break;

            case DeliveryRequested:
                IsDeliveryRequired = true;
                break;

            case SaleFinalized e:
                Status             = SaleStatus.Confirmed;
                ServedBy           = e.ServedBy;
                IsDeliveryRequired = e.IsDeliveryRequired;
                break;

            case SalePaid e:
                Status  = SaleStatus.Paid;
                Payment = new SalePayment(e.PaidAmount, e.Change, e.PaymentMethod, e.PaidAt);
                break;

            case SaleCancelled:
                Status = SaleStatus.Cancelled;
                break;
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void EnsureDraft()
    {
        if (Status != SaleStatus.Draft)
            throw new DomainException($"Operasi ini tidak tersedia pada status '{Status}'.");
    }
}
