using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Purchasing;

namespace Materia.Application.Commands.Purchasing.RegisterSupplier;

public sealed class RegisterSupplierCommandHandler(ISupplierRepository repository)
{
    public async Task<Guid> HandleAsync(RegisterSupplierCommand command, CancellationToken ct = default)
    {
        var supplier = Supplier.Register(
            command.Name, command.ContactPhone, command.Description,
            command.SalesmanName, command.SalesmanPhone, command.CreatedBy);
        await repository.SaveAsync(supplier, ct);
        return supplier.Id.Value;
    }
}
