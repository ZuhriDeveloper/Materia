using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.SetCustomerStatus;

public class SetCustomerStatusCommandHandler(ICustomerRepository repository)
{
    public async Task HandleAsync(SetCustomerStatusCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        if (command.IsActive) customer.Activate(command.UpdatedBy);
        else                  customer.Deactivate(command.UpdatedBy);

        await repository.SaveAsync(customer, ct);
    }
}
