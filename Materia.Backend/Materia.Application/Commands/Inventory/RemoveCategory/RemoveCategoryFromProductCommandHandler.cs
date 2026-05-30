using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.RemoveCategory;

public class RemoveCategoryFromProductCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(RemoveCategoryFromProductCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        product.RemoveCategory(CategoryId.From(command.CategoryId), command.UpdatedBy);
        await repository.SaveAsync(product, ct);
    }
}
