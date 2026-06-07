using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateColorVariant;

public class UpdateColorVariantCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(UpdateColorVariantCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        product.UpdateColorVariant(
            VariantId.From(command.VariantId),
            new Color(command.ColorName, command.ColorCode),
            command.Barcode,
            command.PriceOverride,
            command.UpdatedBy);

        await repository.SaveAsync(product, ct);
    }
}
