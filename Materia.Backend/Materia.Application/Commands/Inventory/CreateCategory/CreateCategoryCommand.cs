namespace Materia.Application.Commands.Inventory.CreateCategory;

public record CreateCategoryCommand(string Name, string? Description, string CreatedBy);
