namespace Materia.Application.Commands.Inventory.UpdateUnit;

public record UpdateUnitCommand(Guid UnitId, string Name, string? Symbol, string UpdatedBy);
