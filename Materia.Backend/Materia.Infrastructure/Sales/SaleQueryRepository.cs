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

        return s is null ? null : Map(s);
    }

    public async Task<PagedResult<SaleDto>> GetPagedAsync(
        int page, int pageSize,
        SaleStatus? status, DateTime? from, DateTime? to,
        CancellationToken ct = default)
    {
        var query = context.SaleReadModels
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (from.HasValue)   query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue)     query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SaleDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    private static SaleDto Map(SaleReadModel s) => new(
        s.Id, s.ReferenceNo,
        s.CustomerId, s.CustomerName,
        s.CustomerAddressId, s.DeliveryAddress,
        s.SaleType, s.Status,
        s.IsDeliveryRequired,
        s.Subtotal, s.GrandTotal,
        s.CreatedBy, s.ServedBy, s.CreatedAt,
        s.PaidAmount.HasValue
            ? new SalePaymentDto(s.PaidAmount.Value, s.Change!.Value, s.PaymentMethod!.Value, s.PaidAt!.Value)
            : null,
        s.Items.Select(i => new SaleItemDto(
            i.Id, i.ProductId, i.ProductName,
            i.UnitName, i.Quantity, i.QuantityInBaseUnit,
            i.UnitPrice, i.Subtotal)).ToList());
}
