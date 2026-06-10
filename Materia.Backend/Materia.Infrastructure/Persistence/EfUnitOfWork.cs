using Materia.Application.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Persistence;

/// <summary>
/// Wraps the action in a database transaction on the scoped <see cref="AppDbContext"/>.
/// Repositories resolved in the same scope share that context, so their individual
/// <c>SaveChangesAsync</c> calls all commit (or roll back) together.
/// </summary>
public class EfUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        // A transaction may already be open (e.g. nested unit-of-work) — just run inline.
        if (context.Database.CurrentTransaction is not null)
        {
            await action();
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        await action();
        await transaction.CommitAsync(ct);
    }
}
