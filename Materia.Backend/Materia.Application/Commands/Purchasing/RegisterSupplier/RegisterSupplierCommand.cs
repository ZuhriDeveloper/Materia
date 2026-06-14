namespace Materia.Application.Commands.Purchasing.RegisterSupplier;

public record RegisterSupplierCommand(
    string Name,
    string? ContactPhone,
    string? Description,
    string? SalesmanName,
    string? SalesmanPhone,
    string CreatedBy);
