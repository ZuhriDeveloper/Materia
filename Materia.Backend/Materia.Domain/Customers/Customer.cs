using Materia.Domain.Common;
using Materia.Domain.Customers.Events;
using Materia.Domain.Sales;

namespace Materia.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    private readonly List<CustomerAddress> _addresses      = [];
    private readonly List<ReceivableLine>  _openReceivables = [];

    public string      Name     { get; private set; } = default!;
    public PhoneNumber Phone    { get; private set; } = default!;
    public string?     Email    { get; private set; }
    public bool        IsActive { get; private set; }

    /// <summary>Total unpaid balance owed by this customer across all credit (bon) sales.</summary>
    public decimal     OutstandingDebt { get; private set; }

    /// <summary>
    /// Open (or partially-paid) receivable lines, oldest first.
    /// Rebuilt by replaying CustomerDebtIncurred and ReceivablePaymentRecorded events.
    /// </summary>
    public IReadOnlyList<ReceivableLine> OpenReceivables
        => _openReceivables.Where(l => l.RemainingAmount > 0)
                           .OrderBy(l => l.IncurredAt)
                           .ToList()
                           .AsReadOnly();

    public IReadOnlyList<CustomerAddress> Addresses => _addresses.AsReadOnly();

    private Customer() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Customer Create(string name, string phone, string? email, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nama pelanggan tidak boleh kosong.");

        var customer = new Customer();
        customer.Raise(new CustomerCreated(
            CustomerId.New(),
            name.Trim(),
            new PhoneNumber(phone).Value,
            email?.Trim(),
            createdBy,
            DateTime.UtcNow));
        return customer;
    }

    public static Customer Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var customer = new Customer();
        customer.Load(events);
        return customer;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void Update(string name, string phone, string? email, string updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat mengubah data pelanggan yang tidak aktif.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nama pelanggan tidak boleh kosong.");

        Raise(new CustomerUpdated(
            Id, name.Trim(), new PhoneNumber(phone).Value,
            email?.Trim(), updatedBy, DateTime.UtcNow));
    }

    public void Activate(string activatedBy)
    {
        if (IsActive) throw new DomainException("Pelanggan sudah aktif.");
        Raise(new CustomerActivated(Id, activatedBy, DateTime.UtcNow));
    }

    /// <summary>
    /// Adds the unpaid remainder of a credit (bon) sale to this customer's outstanding debt.
    /// </summary>
    public void IncurDebt(decimal amount, Guid saleId, string referenceNo, string incurredBy)
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat menambah piutang pada pelanggan yang tidak aktif.");
        if (amount <= 0)
            throw new DomainException("Jumlah piutang harus lebih dari nol.");

        Raise(new CustomerDebtIncurred(
            Id, saleId,
            string.IsNullOrWhiteSpace(referenceNo) ? string.Empty : referenceNo.Trim(),
            amount, OutstandingDebt + amount,
            incurredBy, DateTime.UtcNow));
    }

    /// <summary>
    /// Records a cash collection (pelunasan piutang) against this customer's open receivables.
    /// FIFO allocation (oldest invoice first) is computed at call time and baked into the event
    /// so that replaying events is always deterministic.
    ///
    /// NOTE: repayment is intentionally allowed even when the customer is inactive.
    /// Once a credit sale is made, the debt is real regardless of account status;
    /// blocking repayment would strand the outstanding balance with no way to clear it.
    /// Contrast with IncurDebt, which DOES block inactive customers.
    ///
    /// <paramref name="idempotencyKey"/> is the client-supplied de-duplication token, baked
    /// into the event so the payment projection can reject duplicate submissions. The domain
    /// does not enforce idempotency itself (it holds no key history); enforcement lives in the
    /// projection's unique index. When omitted, a fresh key is generated so internal callers
    /// still produce a unique, non-empty value.
    /// </summary>
    public Guid RecordRepayment(
        decimal amount, PaymentMethod method, string? notes, string receivedBy,
        Guid? idempotencyKey = null)
    {
        if (amount <= 0)
            throw new DomainException("Jumlah pembayaran harus lebih dari nol.");
        if (OutstandingDebt <= 0)
            throw new DomainException("Pelanggan tidak memiliki sisa piutang.");
        if (amount > OutstandingDebt)
            throw new DomainException("Jumlah pembayaran melebihi sisa piutang.");

        // FIFO allocation — oldest open invoices settled first
        var ordered   = OpenReceivables; // already ordered by IncurredAt ascending
        var remaining = amount;
        var allocations = new List<ReceivableAllocation>();

        foreach (var line in ordered)
        {
            if (remaining <= 0) break;

            var applied       = Math.Min(remaining, line.RemainingAmount);
            var remainingAfter = line.RemainingAmount - applied;
            allocations.Add(new ReceivableAllocation(
                line.SaleId, line.ReferenceNo, applied, remainingAfter));
            remaining -= applied;
        }

        // Invariant: OutstandingDebt equals the sum of open-line remainders, and the guard
        // above ensures amount <= OutstandingDebt, so the payment must allocate fully. Fail
        // loudly if a future path ever desyncs them rather than silently swallowing cash.
        if (remaining != 0)
            throw new DomainException(
                "Pembayaran tidak dapat dialokasikan sepenuhnya ke piutang yang terbuka.");

        var paymentId = Guid.NewGuid();
        var key = idempotencyKey is { } k && k != Guid.Empty ? k : Guid.NewGuid();
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Raise(new ReceivablePaymentRecorded(
            Id, paymentId, key, amount,
            OutstandingDebt - amount,
            allocations.AsReadOnly(),
            method, trimmedNotes, receivedBy, DateTime.UtcNow));

        return paymentId;
    }

    public void ReduceDebtForReturn(decimal amount, Guid saleReturnId, string reducedBy)
    {
        if (amount <= 0)
            throw new DomainException("Jumlah pengurangan piutang harus lebih dari nol.");
        if (amount > OutstandingDebt)
            throw new DomainException("Jumlah pengurangan melebihi sisa piutang pelanggan.");

        Raise(new CustomerDebtReducedForReturn(
            Id, saleReturnId, amount,
            OutstandingDebt - amount,
            reducedBy, DateTime.UtcNow));
    }

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive) throw new DomainException("Pelanggan sudah tidak aktif.");
        Raise(new CustomerDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    public AddressId AddAddress(
        string       label,
        string       street,
        string       city,
        string       province,
        string?      postalCode,
        Coordinates? coordinates,
        string       updatedBy,
        string?      subdistrict = null,   // Kelurahan / Desa
        string?      district    = null)   // Kecamatan
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat menambah alamat pada pelanggan yang tidak aktif.");

        ValidateAddressFields(label, street, city, province);

        var isDefault = _addresses.Count == 0;
        var addressId = AddressId.New();

        Raise(new CustomerAddressAdded(
            Id, addressId,
            label.Trim(), street.Trim(), city.Trim(), province.Trim(), postalCode?.Trim(),
            coordinates?.Latitude, coordinates?.Longitude,
            isDefault, updatedBy, DateTime.UtcNow,
            Normalize(subdistrict), Normalize(district)));

        return addressId;
    }

    public void UpdateAddress(
        AddressId    addressId,
        string       label,
        string       street,
        string       city,
        string       province,
        string?      postalCode,
        Coordinates? coordinates,
        string       updatedBy,
        string?      subdistrict = null,   // Kelurahan / Desa
        string?      district    = null)   // Kecamatan
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat mengubah alamat pelanggan yang tidak aktif.");
        if (_addresses.All(a => a.Id != addressId))
            throw new DomainException($"Alamat '{addressId}' tidak ditemukan.");

        ValidateAddressFields(label, street, city, province);

        Raise(new CustomerAddressUpdated(
            Id, addressId,
            label.Trim(), street.Trim(), city.Trim(), province.Trim(), postalCode?.Trim(),
            coordinates?.Latitude, coordinates?.Longitude,
            updatedBy, DateTime.UtcNow,
            Normalize(subdistrict), Normalize(district)));
    }

    public void RemoveAddress(AddressId addressId, string updatedBy)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException($"Alamat '{addressId}' tidak ditemukan.");

        if (address.IsDefault && _addresses.Count > 1)
            throw new DomainException(
                "Tidak dapat menghapus alamat utama selama masih ada alamat lain. " +
                "Tetapkan alamat lain sebagai utama terlebih dahulu.");

        Raise(new CustomerAddressRemoved(Id, addressId, updatedBy, DateTime.UtcNow));
    }

    public void SetDefaultAddress(AddressId addressId, string updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat mengubah alamat utama pelanggan yang tidak aktif.");
        if (_addresses.All(a => a.Id != addressId))
            throw new DomainException($"Alamat '{addressId}' tidak ditemukan.");
        if (_addresses.First(a => a.Id == addressId).IsDefault)
            return; // already default — idempotent

        Raise(new CustomerDefaultAddressChanged(Id, addressId, updatedBy, DateTime.UtcNow));
    }

    // ── Event Application ─────────────────────────────────────────────────────

    protected override void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case CustomerCreated e:
                Id       = e.CustomerId;
                Name     = e.Name;
                Phone    = new PhoneNumber(e.Phone);
                Email    = e.Email;
                IsActive = true;
                break;

            case CustomerUpdated e:
                Name  = e.Name;
                Phone = new PhoneNumber(e.Phone);
                Email = e.Email;
                break;

            case CustomerActivated:
                IsActive = true;
                break;

            case CustomerDebtIncurred e:
                OutstandingDebt = e.NewBalance;
                // Build the open-receivable line so FIFO allocation has the full invoice list.
                // Backward-compatible: existing stored events replay correctly since all
                // required fields (SaleId, ReferenceNo, Amount, OccurredAt) are already present.
                _openReceivables.Add(new ReceivableLine(
                    e.SaleId, e.ReferenceNo,
                    originalAmount:  e.Amount,
                    remainingAmount: e.Amount,
                    incurredAt:      e.OccurredAt));
                break;

            case ReceivablePaymentRecorded e:
                OutstandingDebt = e.NewBalance;
                foreach (var alloc in e.Allocations)
                {
                    var line = _openReceivables.FirstOrDefault(l => l.SaleId == alloc.SaleId);
                    line?.ApplyPayment(alloc.AppliedAmount);
                }
                break;

            case CustomerDebtReducedForReturn e:
                OutstandingDebt = e.OutstandingDebtAfter;
                var remaining = e.ReducedAmount;
                foreach (var line in _openReceivables.OrderBy(l => l.IncurredAt))
                {
                    if (remaining <= 0) break;
                    var applied = Math.Min(remaining, line.RemainingAmount);
                    line.ApplyPayment(applied);
                    remaining -= applied;
                }
                break;

            case CustomerDeactivated:
                IsActive = false;
                break;

            case CustomerAddressAdded e:
                _addresses.Add(new CustomerAddress(
                    e.AddressId, e.Label, e.Street, e.City, e.Province, e.PostalCode,
                    ToCoordinates(e.Latitude, e.Longitude), e.IsDefault,
                    e.Subdistrict, e.District));
                break;

            case CustomerAddressUpdated e:
                _addresses.First(a => a.Id == e.AddressId)
                    .Update(e.Label, e.Street, e.City, e.Province, e.PostalCode,
                            ToCoordinates(e.Latitude, e.Longitude),
                            e.Subdistrict, e.District);
                break;

            case CustomerAddressRemoved e:
                _addresses.RemoveAll(a => a.Id == e.AddressId);
                break;

            case CustomerDefaultAddressChanged e:
                foreach (var addr in _addresses)
                    addr.IsDefault = addr.Id == e.NewDefaultAddressId;
                break;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Trims and collapses blank/whitespace optional fields to null.</summary>
    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Builds a coordinate only when both components are present; otherwise null (no map pin).</summary>
    private static Coordinates? ToCoordinates(decimal? latitude, decimal? longitude)
        => latitude is { } lat && longitude is { } lng ? new Coordinates(lat, lng) : null;

    private static void ValidateAddressFields(
        string label, string street, string city, string province)
    {
        if (string.IsNullOrWhiteSpace(label))    throw new DomainException("Label alamat tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(street))   throw new DomainException("Jalan tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(city))     throw new DomainException("Kota tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(province)) throw new DomainException("Provinsi tidak boleh kosong.");
    }
}
