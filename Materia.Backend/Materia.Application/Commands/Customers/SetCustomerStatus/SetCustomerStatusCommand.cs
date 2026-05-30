namespace Materia.Application.Commands.Customers.SetCustomerStatus;

public record SetCustomerStatusCommand(Guid CustomerId, bool IsActive, string UpdatedBy);
