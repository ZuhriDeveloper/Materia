using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.SetDefaultAddress;

public class SetDefaultAddressCommandHandler(ICustomerRepository repository)
{
    public async Task HandleAsync(SetDefaultAddressCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        customer.SetDefaultAddress(AddressId.From(command.AddressId), command.UpdatedBy);
        await repository.SaveAsync(customer, ct);
    }
}
