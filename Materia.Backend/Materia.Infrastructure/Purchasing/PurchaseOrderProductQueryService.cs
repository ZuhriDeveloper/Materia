using Materia.Application.Contracts.Purchasing;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

public class PurchaseOrderProductQueryService(AppDbContext context) : IPurchaseOrderProductQueryService
{
    public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default)
    {
        // LinesJson is a JSONB array of line objects each carrying a "productId" field.
        // Use JSONB containment (@>) — valid on jsonb (LIKE is not) and matches the field
        // rather than a blind substring. System.Text.Json serializes the Guid lowercase,
        // which matches Guid.ToString(); jsonb string comparison is exact.
        var contains = $"[{{\"productId\":\"{productId}\"}}]";
        return context.PurchaseOrderReadModels
            .AnyAsync(po => EF.Functions.JsonContains(po.LinesJson, contains), ct);
    }
}
