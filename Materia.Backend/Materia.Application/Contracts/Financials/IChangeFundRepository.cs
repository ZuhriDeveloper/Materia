using Materia.Domain.Financials;

namespace Materia.Application.Contracts.Financials;

public interface IChangeFundRepository
{
    /// <exception cref="DuplicateChangeFundEntryException">
    /// A deposit with the same idempotency key was already persisted (concurrent duplicate).
    /// </exception>
    Task SaveAsync(ChangeFundDeposit deposit, CancellationToken ct = default);

    /// <exception cref="DuplicateChangeFundEntryException">
    /// A withdrawal with the same idempotency key was already persisted (concurrent duplicate).
    /// </exception>
    Task SaveAsync(ChangeFundWithdrawal withdrawal, CancellationToken ct = default);

    /// <summary>Id of the previously recorded deposit with this client key, for response replay.</summary>
    Task<Guid?> FindDepositIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default);

    /// <summary>Id of the previously recorded withdrawal with this client key, for response replay.</summary>
    Task<Guid?> FindWithdrawalIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default);
}
