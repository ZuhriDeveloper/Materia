namespace Materia.Application.Commands.Customers.UpdateCustomerAddress;

public record UpdateCustomerAddressCommand(
    Guid    CustomerId,
    Guid    AddressId,
    string  Label,
    string  Street,
    string  City,
    string  Province,
    string?  PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string   UpdatedBy,
    string? Subdistrict = null,   // Kelurahan / Desa
    string? District    = null);  // Kecamatan
