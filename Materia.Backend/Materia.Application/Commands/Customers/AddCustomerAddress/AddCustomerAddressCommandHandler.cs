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

        var coordinates = command.Latitude is { } lat && command.Longitude is { } lng
            ? new Coordinates(lat, lng)
            : null;

        var addressId = customer.AddAddress(
            command.Label, command.Street, command.City,
            command.Province, command.PostalCode,
            coordinates,
            command.UpdatedBy,
            command.Subdistrict, command.District);

        await repository.SaveAsync(customer, ct);
        return addressId.Value;
    }
}
