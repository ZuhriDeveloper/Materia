using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.AddCustomerAddress;

public class AddCustomerAddressCommandHandler(ICustomerRepository repository)
{
    public async Task<Guid> HandleAsync(AddCustomerAddressCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        var addressId = customer.AddAddress(
            command.Label, command.Street, command.City,
            command.Province, command.PostalCode,
            new Coordinates(command.Latitude, command.Longitude),
            command.UpdatedBy);

        await repository.SaveAsync(customer, ct);
        return addressId.Value;
    }
}
