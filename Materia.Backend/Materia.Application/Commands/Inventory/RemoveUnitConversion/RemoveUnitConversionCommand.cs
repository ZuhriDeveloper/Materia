namespace Materia.Application.Commands.Inventory.RemoveUnitConversion;

public record RemoveUnitConversionCommand(Guid ProductId, string ToUnit, string UpdatedBy);
