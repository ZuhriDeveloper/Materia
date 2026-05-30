using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.RemoveUnitConversion;

public class RemoveUnitConversionCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(RemoveUnitConversionCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        product.RemoveUnitConversion(new UnitName(command.ToUnit), command.UpdatedBy);
        await repository.SaveAsync(product, ct);
    }
}
