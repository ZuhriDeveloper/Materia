using Materia.Application.Contracts.Customers;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.CreateCustomer;

public class CreateCustomerCommandHandler(ICustomerRepository repository)
{
    public async Task<Guid> HandleAsync(CreateCustomerCommand command, CancellationToken ct = default)
    {
        var customer = Customer.Create(command.Name, command.Phone, command.Email, command.CreatedBy);
        await repository.SaveAsync(customer, ct);
        return customer.Id.Value;
    }
}
