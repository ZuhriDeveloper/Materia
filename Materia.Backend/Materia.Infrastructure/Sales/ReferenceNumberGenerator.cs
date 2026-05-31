using Materia.Application.Contracts.Sales;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class ReferenceNumberGenerator(AppDbContext context) : IReferenceNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var today  = DateTime.UtcNow;
        var prefix = $"SO-{today:yyyyMMdd}";

        var count = await context.SaleReadModels
            .CountAsync(s => s.ReferenceNo.StartsWith(prefix), ct);

        return $"{prefix}-{count + 1:D4}";  // e.g. SO-20260531-0001
    }
}
