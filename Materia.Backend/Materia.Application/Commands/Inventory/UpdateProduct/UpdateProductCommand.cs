namespace Materia.Application.Commands.Inventory.UpdateProduct;

public record UpdateProductCommand(Guid ProductId, string Name, string? Description, string UpdatedBy);
