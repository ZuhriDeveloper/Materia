namespace Materia.Application.Commands.Customers.RemoveCustomerAddress;

public record RemoveCustomerAddressCommand(Guid CustomerId, Guid AddressId, string UpdatedBy);
