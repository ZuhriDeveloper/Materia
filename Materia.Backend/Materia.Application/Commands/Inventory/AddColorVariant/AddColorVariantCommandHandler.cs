using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.AddColorVariant;

public class AddColorVariantCommandHandler(IProductRepository repository)
{
    public async Task<Guid> HandleAsync(AddColorVariantCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        var variantId = product.AddColorVariant(
            new Color(command.ColorName, command.ColorCode),
            command.Barcode,
            command.PriceOverride,
            command.UpdatedBy);

        await repository.SaveAsync(product, ct);
        return variantId.Value;
    }
}
