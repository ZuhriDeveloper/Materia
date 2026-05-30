using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.UpdateCustomerAddress;

public class UpdateCustomerAddressCommandHandler(ICustomerRepository repository)
{
    public async Task HandleAsync(UpdateCustomerAddressCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        customer.UpdateAddress(
            AddressId.From(command.AddressId),
            command.Label, command.Street, command.City,
            command.Province, command.PostalCode,
            new Coordinates(command.Latitude, command.Longitude),
            command.UpdatedBy);

        await repository.SaveAsync(customer, ct);
    }
}
