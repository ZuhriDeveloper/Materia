using Materia.Domain.Customers;

namespace Materia.Application.Contracts.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
    Task SaveAsync(Customer customer, CancellationToken ct = default);
}
