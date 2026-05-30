namespace Materia.Application.Commands.Inventory.UpdateCategory;

public record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description, string UpdatedBy);
