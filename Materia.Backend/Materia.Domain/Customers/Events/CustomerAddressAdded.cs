using Materia.Domain.Common;

namespace Materia.Domain.Customers.Events;

public record CustomerAddressAdded(
    CustomerId CustomerId,
    AddressId  AddressId,
    string     Label,
    string     Street,
    string     City,
    string     Province,
    string?    PostalCode,
    decimal?   Latitude,
    decimal?   Longitude,
    bool       IsDefault,
    string     UpdatedBy,
    DateTime   OccurredAt,
    // Appended (with defaults) so older stored events stay deserializable.
    string?    Subdistrict = null,   // Kelurahan / Desa
    string?    District    = null    // Kecamatan
    ) : IDomainEvent;
