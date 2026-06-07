namespace Materia.Application.Commands.Inventory.AdjustStock;

public record AdjustStockCommand(
    Guid ProductId, decimal Delta, string? Reason, string AdjustedBy, Guid? VariantId = null);
