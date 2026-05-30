namespace Materia.Application.Commands.Inventory.SetUnitStatus;

public record SetUnitStatusCommand(Guid UnitId, bool IsActive, string UpdatedBy);
