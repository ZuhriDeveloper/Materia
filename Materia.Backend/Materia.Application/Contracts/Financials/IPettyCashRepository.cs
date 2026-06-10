using Materia.Domain.Financials;

namespace Materia.Application.Contracts.Financials;

public interface IPettyCashRepository
{
    Task<PettyCashExpense?> GetByIdAsync(PettyCashExpenseId id, CancellationToken ct = default);

    /// <exception cref="DuplicatePettyCashExpenseException">
    /// An expense with the same idempotency key was already persisted (concurrent duplicate).
    /// </exception>
    Task SaveAsync(PettyCashExpense expense, CancellationToken ct = default);

    /// <summary>Id of the previously recorded expense with this client key, for response replay.</summary>
    Task<Guid?> FindExpenseIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default);
}
