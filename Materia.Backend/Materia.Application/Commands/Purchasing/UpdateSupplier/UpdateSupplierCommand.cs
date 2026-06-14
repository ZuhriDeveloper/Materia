namespace Materia.Application.Commands.Purchasing.UpdateSupplier;

public record UpdateSupplierCommand(
    Guid SupplierId,
    string Name,
    string? ContactPhone,
    string? Description,
    string? SalesmanName,
    string? SalesmanPhone,
    string UpdatedBy);
