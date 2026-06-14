using System.Text.Json;
using Materia.Application.Contracts.Purchasing;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Purchasing;

public class PurchaseOrderQueryRepository(AppDbContext context) : IPurchaseOrderQueryRepository
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await context.PurchaseOrderReadModels
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(MapToDto).ToList();
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await context.PurchaseOrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return row is null ? null : MapToDto(row);
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrderReadModel row)
    {
        var lines = JsonSerializer.Deserialize<List<LineJson>>(row.LinesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? [];

        return new PurchaseOrderDto(
            row.Id,
            row.SupplierId,
            row.SupplierName,
            row.Status,
            lines.Select(l => new PurchaseOrderLineDto(
                l.ProductId, null, l.OrderedQty, l.ReceivedQty, l.ReturnedQty, l.UnitCost, l.Unit,
                l.ListUnitCost ?? l.UnitCost, l.Discounts ?? []))
                .ToList(),
            row.CreatedBy,
            row.CreatedAt,
            row.ReceivedAt,
            row.PaymentTermValue,
            row.PaymentTermUnit,
            row.PaymentDueDate);
    }

    private sealed record LineJson(
        Guid ProductId,
        decimal OrderedQty,
        decimal ReceivedQty,
        decimal ReturnedQty,
        decimal UnitCost,
        string Unit,
        decimal? ListUnitCost = null,
        IReadOnlyList<decimal>? Discounts = null);
}
