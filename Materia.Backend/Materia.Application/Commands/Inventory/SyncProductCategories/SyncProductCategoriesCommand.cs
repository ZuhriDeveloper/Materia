namespace Materia.Application.Commands.Inventory.SyncProductCategories;

public record SyncProductCategoriesCommand(
    Guid ProductId,
    IReadOnlyList<Guid> CategoryIds,
    string UpdatedBy);
