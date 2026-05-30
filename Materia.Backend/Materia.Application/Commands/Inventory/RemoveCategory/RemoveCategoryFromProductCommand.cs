namespace Materia.Application.Commands.Inventory.RemoveCategory;

public record RemoveCategoryFromProductCommand(Guid ProductId, Guid CategoryId, string UpdatedBy);
