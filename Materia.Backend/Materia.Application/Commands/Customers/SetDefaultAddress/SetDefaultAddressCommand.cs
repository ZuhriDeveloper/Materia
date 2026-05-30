namespace Materia.Application.Commands.Customers.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid CustomerId, Guid AddressId, string UpdatedBy);
