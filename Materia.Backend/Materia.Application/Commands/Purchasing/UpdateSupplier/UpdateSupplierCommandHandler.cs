using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.UpdateSupplier;

public sealed class UpdateSupplierCommandHandler(ISupplierRepository repository)
{
    public async Task HandleAsync(UpdateSupplierCommand command, CancellationToken ct = default)
    {
        var supplier = await repository.GetByIdAsync(SupplierId.From(command.SupplierId), ct)
            ?? throw new DomainException($"Supplier {command.SupplierId} not found.");

        supplier.Update(command.Name, command.ContactPhone, command.UpdatedBy);
        await repository.SaveAsync(supplier, ct);
    }
}
