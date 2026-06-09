namespace Materia.Application.DTOs.Customers;

public record CustomerAddressDto(
    Guid    Id,
    string  Label,
    string  Street,
    string  City,
    string  Province,
    string?  PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    bool     IsDefault,
    string?  Subdistrict = null,   // Kelurahan / Desa
    string?  District    = null);  // Kecamatan

public record CustomerDto(
    Guid     Id,
    string   Name,
    string   Phone,
    string?  Email,
    bool     IsActive,
    decimal  OutstandingDebt,
    string   CreatedBy,
    DateTime CreatedAt,
    string?  UpdatedBy,
    DateTime? UpdatedAt,
    IReadOnlyList<CustomerAddressDto> Addresses);

public record NearbyCustomerDto(
    Guid   CustomerId,
    string Name,
    string Phone,
    Guid   AddressId,
    string AddressLabel,
    string FullAddress,
    decimal Latitude,
    decimal Longitude,
    double DistanceKm);
