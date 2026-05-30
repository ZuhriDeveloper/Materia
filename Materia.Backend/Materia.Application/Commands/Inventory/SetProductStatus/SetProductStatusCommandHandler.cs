using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.SetProductStatus;

public class SetProductStatusCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(SetProductStatusCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        if (command.IsActive)
            product.Activate(command.UpdatedBy);
        else
            product.Deactivate(command.UpdatedBy);

        await repository.SaveAsync(product, ct);
    }
}
