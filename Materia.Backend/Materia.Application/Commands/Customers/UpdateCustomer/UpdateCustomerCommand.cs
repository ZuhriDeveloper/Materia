namespace Materia.Application.Commands.Customers.UpdateCustomer;

public record UpdateCustomerCommand(Guid CustomerId, string Name, string Phone, string? Email, string UpdatedBy);
