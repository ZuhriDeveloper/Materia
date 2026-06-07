namespace Materia.Application.Commands.Inventory.SetColorVariantStatus;

public record SetColorVariantStatusCommand(Guid ProductId, Guid VariantId, bool IsActive, string UpdatedBy);
