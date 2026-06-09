using Materia.Application.Contracts.Financials;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Financials;

public class PettyCashReferenceNumberGenerator(AppDbContext context) : IPettyCashReferenceGenerator
{
    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var today  = DateTime.UtcNow;
        var prefix = $"KK-{today:yyyyMMdd}";

        var count = await context.PettyCashExpenseReadModels
            .CountAsync(e => e.ReferenceNo.StartsWith(prefix), ct);

        return $"{prefix}-{count + 1:D4}";  // e.g. KK-20260609-0001
    }
}
