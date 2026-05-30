namespace Materia.Application.Commands.Customers.AddCustomerAddress;

public record AddCustomerAddressCommand(
    Guid    CustomerId,
    string  Label,
    string  Street,
    string  City,
    string  Province,
    string? PostalCode,
    decimal Latitude,
    decimal Longitude,
    string  UpdatedBy);
