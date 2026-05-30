using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.CreateProduct;

public class CreateProductCommandHandler(IProductRepository repository)
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = Product.Create(command.Name, command.Description, new UnitName(command.BaseUnit), command.CreatedBy);
        await repository.SaveAsync(product, ct);
        return product.Id.Value;
    }
}
