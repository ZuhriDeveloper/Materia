using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.RemoveColorVariant;

public class RemoveColorVariantCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(RemoveColorVariantCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        product.RemoveColorVariant(VariantId.From(command.VariantId), command.UpdatedBy);
        await repository.SaveAsync(product, ct);
    }
}
