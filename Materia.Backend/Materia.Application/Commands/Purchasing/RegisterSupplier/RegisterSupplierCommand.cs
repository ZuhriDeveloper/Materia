namespace Materia.Application.Commands.Purchasing.RegisterSupplier;

public record RegisterSupplierCommand(
    string Name,
    string? ContactPhone,
    string CreatedBy);
