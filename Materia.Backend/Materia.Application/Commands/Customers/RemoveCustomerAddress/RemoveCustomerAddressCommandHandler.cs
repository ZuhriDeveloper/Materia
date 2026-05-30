using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.RemoveCustomerAddress;

public class RemoveCustomerAddressCommandHandler(ICustomerRepository repository)
{
    public async Task HandleAsync(RemoveCustomerAddressCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        customer.RemoveAddress(AddressId.From(command.AddressId), command.UpdatedBy);
        await repository.SaveAsync(customer, ct);
    }
}
