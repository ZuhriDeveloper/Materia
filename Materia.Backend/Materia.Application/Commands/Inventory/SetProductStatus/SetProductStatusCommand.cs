namespace Materia.Application.Commands.Inventory.SetProductStatus;

public record SetProductStatusCommand(Guid ProductId, bool IsActive, string UpdatedBy);
