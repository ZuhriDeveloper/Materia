using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository repository,
    ICategoryRepository categoryRepository,
    IStockRepository stockRepository)
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = Product.Create(command.Name, command.Description, new UnitName(command.BaseUnit), command.CreatedBy);

        foreach (var categoryId in command.CategoryIds)
        {
            var catId = CategoryId.From(categoryId);
            if (!await categoryRepository.ExistsAsync(catId, ct))
                throw new DomainException($"Category '{categoryId}' not found.");
            product.AssignCategory(catId, command.CreatedBy);
        }

        await repository.SaveAsync(product, ct);

        var stock = Stock.Initialize(product.Id, command.BaseUnit, command.CreatedBy);
        await stockRepository.SaveAsync(stock, ct);

        return product.Id.Value;
    }
}
