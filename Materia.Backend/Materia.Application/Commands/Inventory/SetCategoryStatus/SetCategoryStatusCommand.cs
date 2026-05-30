namespace Materia.Application.Commands.Inventory.SetCategoryStatus;

public record SetCategoryStatusCommand(Guid CategoryId, bool IsActive, string UpdatedBy);
