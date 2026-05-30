namespace Materia.Application.Commands.Inventory.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    string BaseUnit,
    string CreatedBy);
