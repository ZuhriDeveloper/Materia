using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.SetSupplierStatus;

public sealed class SetSupplierStatusCommandHandler(ISupplierRepository repository)
{
    public async Task HandleAsync(SetSupplierStatusCommand command, CancellationToken ct = default)
    {
        var supplier = await repository.GetByIdAsync(SupplierId.From(command.SupplierId), ct)
            ?? throw new DomainException($"Supplier {command.SupplierId} not found.");

        if (command.IsActive)
            supplier.Activate(command.ChangedBy);
        else
            supplier.Deactivate(command.ChangedBy);

        await repository.SaveAsync(supplier, ct);
    }
}
