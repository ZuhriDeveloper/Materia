using Materia.Application.Contracts.Sales;
using Materia.Application.DTOs.Inventory;
using Materia.Application.DTOs.Sales;
using Materia.Domain.Sales;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class SaleQueryRepository(AppDbContext context) : ISaleQueryRepository
{
    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await context.SaleReadModels
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null) return null;

        var returnedAmount = await context.SaleReturnReadModels
            .Where(r => r.OriginalSaleId == id)
            .SumAsync(r => (decimal?)r.TotalRefundAmount, ct) ?? 0m;

        return Map(s, returnedAmount);
    }

    public async Task<PagedResult<SaleDto>> GetPagedAsync(
        int page, int pageSize,
        SaleStatus? status, DateTime? from, DateTime? to,
        string? customerName = null, SaleType? saleType = null, string? referenceNo = null,
        CancellationToken ct = default)
    {
        var query = context.SaleReadModels
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        if (status.HasValue)                    query = query.Where(x => x.Status == status.Value);
        if (from.HasValue)                      query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue)                        query = query.Where(x => x.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(customerName)) query = query.Where(x => x.CustomerName.Contains(customerName));
        if (saleType.HasValue)                  query = query.Where(x => x.SaleType == saleType.Value);
        if (!string.IsNullOrEmpty(referenceNo)) query = query.Where(x => x.ReferenceNo.Contains(referenceNo));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Returned amount per sale, scoped to just this page's ids (single grouped query).
        var ids = items.Select(i => i.Id).ToList();
        var returnedBySale = await context.SaleReturnReadModels
            .Where(r => ids.Contains(r.OriginalSaleId))
            .GroupBy(r => r.OriginalSaleId)
            .Select(g => new { SaleId = g.Key, Total = g.Sum(x => x.TotalRefundAmount) })
            .ToDictionaryAsync(x => x.SaleId, x => x.Total, ct);

        return new PagedResult<SaleDto>(
            items.Select(s => Map(s, returnedBySale.GetValueOrDefault(s.Id, 0m))).ToList(),
            total, page, pageSize);
    }

    private static SaleDto Map(SaleReadModel s, decimal returnedAmount = 0m) => new(
        s.Id, s.ReferenceNo,
        s.CustomerId, s.CustomerName,
        s.CustomerAddressId, s.DeliveryAddress,
        s.SaleType, s.Status,
        s.IsDeliveryRequired,
        s.Subtotal, s.Discount, s.Tax, s.GrandTotal,
        s.AmountPaid, s.OutstandingAmount,
        s.CreatedBy, s.ServedBy, s.CreatedAt,
        s.PaidAmount.HasValue
            ? new SalePaymentDto(
                s.PaidAmount.Value, s.Change!.Value, s.OutstandingAmount,
                s.PaymentMethod!.Value, s.PaidAt!.Value)
            : null,
        s.Items.Select(i => new SaleItemDto(
            i.Id, i.ProductId, i.ProductName,
            i.UnitName, i.Quantity, i.QuantityInBaseUnit,
            i.UnitPrice, i.Subtotal, i.VariantId, i.ColorName)).ToList(),
        returnedAmount);
}
