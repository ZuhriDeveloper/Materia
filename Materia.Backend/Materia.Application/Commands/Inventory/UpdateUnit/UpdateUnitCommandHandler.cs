using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateUnit;

public class UpdateUnitCommandHandler(IUnitRepository repository)
{
    public async Task HandleAsync(UpdateUnitCommand command, CancellationToken ct = default)
    {
        var unit = await repository.GetByIdAsync(UnitId.From(command.UnitId), ct)
            ?? throw new DomainException($"Unit '{command.UnitId}' not found.");

        unit.Update(command.Name, command.Symbol, command.UpdatedBy);
        await repository.SaveAsync(unit, ct);
    }
}
