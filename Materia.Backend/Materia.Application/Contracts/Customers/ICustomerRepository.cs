using Materia.Domain.Customers;

namespace Materia.Application.Contracts.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
    Task SaveAsync(Customer customer, CancellationToken ct = default);

    /// <summary>
    /// Returns a previously recorded receivable payment for the given client idempotency key,
    /// or <c>null</c> if none exists. Used to short-circuit duplicate/retried submissions.
    /// </summary>
    Task<StoredReceivablePayment?> FindReceivablePaymentByKeyAsync(
        Guid idempotencyKey, CancellationToken ct = default);
}
