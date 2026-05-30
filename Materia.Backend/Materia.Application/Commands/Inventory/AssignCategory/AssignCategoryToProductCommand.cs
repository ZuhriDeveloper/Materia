namespace Materia.Application.Commands.Inventory.AssignCategory;

public record AssignCategoryToProductCommand(Guid ProductId, Guid CategoryId, string UpdatedBy);
