using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.AssignCategory;

public class AssignCategoryToProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository)
{
    public async Task HandleAsync(AssignCategoryToProductCommand command, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        var categoryId = CategoryId.From(command.CategoryId);
        if (!await categoryRepository.ExistsAsync(categoryId, ct))
            throw new DomainException($"Category '{command.CategoryId}' not found.");

        product.AssignCategory(categoryId, command.UpdatedBy);
        await productRepository.SaveAsync(product, ct);
    }
}
