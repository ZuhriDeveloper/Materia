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

        var coordinates = command.Latitude is { } lat && command.Longitude is { } lng
            ? new Coordinates(lat, lng)
            : null;

        customer.UpdateAddress(
            AddressId.From(command.AddressId),
            command.Label, command.Street, command.City,
            command.Province, command.PostalCode,
            coordinates,
            command.UpdatedBy,
            command.Subdistrict, command.District);

        await repository.SaveAsync(customer, ct);
    }
}
