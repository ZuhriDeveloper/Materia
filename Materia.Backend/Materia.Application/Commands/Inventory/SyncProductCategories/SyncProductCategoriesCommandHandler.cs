using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.SyncProductCategories;

public class SyncProductCategoriesCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(SyncProductCategoriesCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        var targetIds  = command.CategoryIds.Select(CategoryId.From).ToHashSet();
        var currentIds = product.CategoryIds.ToHashSet();

        foreach (var id in currentIds.Where(id => !targetIds.Contains(id)))
            product.RemoveCategory(id, command.UpdatedBy);

        foreach (var id in targetIds.Where(id => !currentIds.Contains(id)))
            product.AssignCategory(id, command.UpdatedBy);

        await repository.SaveAsync(product, ct);
    }
}
