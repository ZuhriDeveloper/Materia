using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.AddUnitConversion;

public class AddUnitConversionCommandHandler(IProductRepository repository)
{
    public async Task HandleAsync(AddUnitConversionCommand command, CancellationToken ct = default)
    {
        var product = await repository.GetByIdAsync(ProductId.From(command.ProductId), ct)
            ?? throw new DomainException($"Product '{command.ProductId}' not found.");

        var conversion = new UnitConversion(product.BaseUnit, new UnitName(command.ToUnit), command.Factor);
        product.AddUnitConversion(conversion, command.UpdatedBy);
        await repository.SaveAsync(product, ct);
    }
}
