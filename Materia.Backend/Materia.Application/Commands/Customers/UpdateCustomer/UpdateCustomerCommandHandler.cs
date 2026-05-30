using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.UpdateCustomer;

public class UpdateCustomerCommandHandler(ICustomerRepository repository)
{
    public async Task HandleAsync(UpdateCustomerCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(CustomerId.From(command.CustomerId), ct)
            ?? throw new DomainException($"Pelanggan '{command.CustomerId}' tidak ditemukan.");

        customer.Update(command.Name, command.Phone, command.Email, command.UpdatedBy);
        await repository.SaveAsync(customer, ct);
    }
}
