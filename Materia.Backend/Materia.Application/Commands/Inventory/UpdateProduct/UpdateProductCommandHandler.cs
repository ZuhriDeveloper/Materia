using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateProduct;

public class UpdateProductCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(UpdateProductCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        product.Update(
            command.Name, command.Description, command.UpdatedBy,
            command.SalePrice, command.Barcode);
        await repository.SaveAsync(product, ct);
    }
}
