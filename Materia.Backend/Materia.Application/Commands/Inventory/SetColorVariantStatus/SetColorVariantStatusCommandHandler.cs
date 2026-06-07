using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.SetColorVariantStatus;

public class SetColorVariantStatusCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(SetColorVariantStatusCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        var variantId = VariantId.From(command.VariantId);
        if (command.IsActive)
            product.ActivateColorVariant(variantId, command.UpdatedBy);
        else
            product.DeactivateColorVariant(variantId, command.UpdatedBy);

        await repository.SaveAsync(product, ct);
    }
}
