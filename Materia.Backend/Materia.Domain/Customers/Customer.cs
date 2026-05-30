using Materia.Domain.Common;
using Materia.Domain.Customers.Events;

namespace Materia.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    private readonly List<CustomerAddress> _addresses = [];

    public string      Name     { get; private set; } = default!;
    public PhoneNumber Phone    { get; private set; } = default!;
    public string?     Email    { get; private set; }
    public bool        IsActive { get; private set; }

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

    public void Deactivate(string deactivatedBy)
    {
        if (!IsActive) throw new DomainException("Pelanggan sudah tidak aktif.");
        Raise(new CustomerDeactivated(Id, deactivatedBy, DateTime.UtcNow));
    }

    public AddressId AddAddress(
        string      label,
        string      street,
        string      city,
        string      province,
        string?     postalCode,
        Coordinates coordinates,
        string      updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat menambah alamat pada pelanggan yang tidak aktif.");

        ValidateAddressFields(label, street, city, province);

        var isDefault = _addresses.Count == 0;
        var addressId = AddressId.New();

        Raise(new CustomerAddressAdded(
            Id, addressId,
            label.Trim(), street.Trim(), city.Trim(), province.Trim(), postalCode?.Trim(),
            coordinates.Latitude, coordinates.Longitude,
            isDefault, updatedBy, DateTime.UtcNow));

        return addressId;
    }

    public void UpdateAddress(
        AddressId   addressId,
        string      label,
        string      street,
        string      city,
        string      province,
        string?     postalCode,
        Coordinates coordinates,
        string      updatedBy)
    {
        if (!IsActive)
            throw new DomainException("Tidak dapat mengubah alamat pelanggan yang tidak aktif.");
        if (_addresses.All(a => a.Id != addressId))
            throw new DomainException($"Alamat '{addressId}' tidak ditemukan.");

        ValidateAddressFields(label, street, city, province);

        Raise(new CustomerAddressUpdated(
            Id, addressId,
            label.Trim(), street.Trim(), city.Trim(), province.Trim(), postalCode?.Trim(),
            coordinates.Latitude, coordinates.Longitude,
            updatedBy, DateTime.UtcNow));
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

            case CustomerDeactivated:
                IsActive = false;
                break;

            case CustomerAddressAdded e:
                _addresses.Add(new CustomerAddress(
                    e.AddressId, e.Label, e.Street, e.City, e.Province, e.PostalCode,
                    new Coordinates(e.Latitude, e.Longitude), e.IsDefault));
                break;

            case CustomerAddressUpdated e:
                _addresses.First(a => a.Id == e.AddressId)
                    .Update(e.Label, e.Street, e.City, e.Province, e.PostalCode,
                            new Coordinates(e.Latitude, e.Longitude));
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

    private static void ValidateAddressFields(
        string label, string street, string city, string province)
    {
        if (string.IsNullOrWhiteSpace(label))    throw new DomainException("Label alamat tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(street))   throw new DomainException("Jalan tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(city))     throw new DomainException("Kota tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(province)) throw new DomainException("Provinsi tidak boleh kosong.");
    }
}
