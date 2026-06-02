namespace Materia.Application.Commands.Inventory.UpdateProduct;

public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    string UpdatedBy,
    decimal SalePrice = 0m,
    string? Barcode = null);
