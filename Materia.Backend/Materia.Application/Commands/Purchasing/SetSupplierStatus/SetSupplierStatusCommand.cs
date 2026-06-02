namespace Materia.Application.Commands.Purchasing.SetSupplierStatus;

public record SetSupplierStatusCommand(Guid SupplierId, bool IsActive, string ChangedBy);
