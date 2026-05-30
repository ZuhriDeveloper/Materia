using Materia.Application.Contracts.Inventory;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.CreateUnit;

public class CreateUnitCommandHandler(IUnitRepository repository)
{
    public async Task<Guid> HandleAsync(CreateUnitCommand command, CancellationToken ct = default)
    {
        var unit = Unit.Create(command.Name, command.Symbol, command.CreatedBy);
        await repository.SaveAsync(unit, ct);
        return unit.Id.Value;
    }
}
