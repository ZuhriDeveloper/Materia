namespace Materia.Application.Commands.Inventory.AddUnitConversion;

public record AddUnitConversionCommand(
    Guid ProductId,
    string ToUnit,
    decimal Factor,
    string UpdatedBy,
    decimal SalePrice = 0m);
