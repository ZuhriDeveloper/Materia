using Materia.Application.Contracts.Sales;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class SaleReturnQueryRepository(AppDbContext context) : ISaleReturnQueryRepository
{
    public async Task<IReadOnlyList<SaleReturnSummary>> GetBySaleIdAsync(
        Guid saleId, CancellationToken ct = default)
    {
        var returns = await context.SaleReturnReadModels
            .Include(r => r.Lines)
            .Where(r => r.OriginalSaleId == saleId)
            .OrderByDescending(r => r.ReturnedAt)
            .ToListAsync(ct);

        return returns.Select(r => new SaleReturnSummary(
            r.Id,
            r.OriginalReferenceNo,
            r.TotalRefundAmount,
            r.Resolution.ToString(),
            r.Reason,
            r.ReturnedBy,
            r.ReturnedAt,
            r.Lines.Select(l => new SaleReturnLineSummary(
                l.ProductId, l.ProductName, l.VariantId, l.ColorName,
                l.UnitName, l.Quantity, l.UnitPrice, l.Subtotal)).ToList()))
            .ToList();
    }
}
