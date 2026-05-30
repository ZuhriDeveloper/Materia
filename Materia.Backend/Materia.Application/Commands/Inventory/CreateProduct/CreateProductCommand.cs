namespace Materia.Application.Commands.Inventory.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    string BaseUnit,
    IReadOnlyList<Guid> CategoryIds,
    string CreatedBy);
