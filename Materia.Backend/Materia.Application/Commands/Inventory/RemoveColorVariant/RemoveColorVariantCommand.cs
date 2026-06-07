namespace Materia.Application.Commands.Inventory.RemoveColorVariant;

public record RemoveColorVariantCommand(Guid ProductId, Guid VariantId, string UpdatedBy);
