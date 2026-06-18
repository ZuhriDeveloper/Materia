namespace Materia.Application.Commands.Inventory.ChangeProductBaseUnit;

public record ChangeProductBaseUnitCommand(
    Guid ProductId,
    string BaseUnit,
    string ChangedBy);
