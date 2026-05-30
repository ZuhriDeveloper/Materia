namespace Materia.Application.Commands.Customers.CreateCustomer;

public record CreateCustomerCommand(string Name, string Phone, string? Email, string CreatedBy);
