using Materia.Application.Contracts.Inventory;
using Materia.Domain.Common;
using Materia.Domain.Inventory;

namespace Materia.Application.Commands.Inventory.SetUnitStatus;

public class SetUnitStatusCommandHandler(IUnitRepository repository)
{
    public async Task HandleAsync(SetUnitStatusCommand command, CancellationToken ct = default)
    {
        var unit = await repository.GetByIdAsync(UnitId.From(command.UnitId), ct)
            ?? throw new DomainException($"Unit '{command.UnitId}' not found.");

        if (command.IsActive)
            unit.Activate(command.UpdatedBy);
        else
            unit.Deactivate(command.UpdatedBy);

        await repository.SaveAsync(unit, ct);
    }
}
