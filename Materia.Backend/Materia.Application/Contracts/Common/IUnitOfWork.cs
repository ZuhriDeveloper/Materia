namespace Materia.Application.Contracts.Common;

/// <summary>
/// Runs a handler body as a single atomic unit. Repositories that share the same
/// underlying persistence context commit together — either every save inside
/// <see cref="ExecuteInTransactionAsync"/> is persisted, or none are.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
