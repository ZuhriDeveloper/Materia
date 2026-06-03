using System.Text.Json;
using Materia.Application.Contracts.Financials;
using Materia.Domain.Sales;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Financials;

public sealed class FinancialQueryRepository(AppDbContext context) : IFinancialQueryRepository
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // ── Profit & Loss ─────────────────────────────────────────────────────────

    public async Task<ProfitAndLossDto> GetProfitAndLossAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Revenue — all completed sales in the period
        var salesRaw = await context.SaleReadModels
            .AsNoTracking()
            .Where(s => s.CreatedAt >= from && s.CreatedAt <= to
                     && s.Status != SaleStatus.Draft
                     && s.Status != SaleStatus.Cancelled)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new { s.ReferenceNo, s.CustomerName, s.CreatedAt, s.GrandTotal })
            .ToListAsync(ct);

        // COGS — received purchase orders in the period (supplier cost)
        var posRaw = await context.PurchaseOrderReadModels
            .AsNoTracking()
            .Where(p => p.Status == "Received"
                     && p.ReceivedAt.HasValue
                     && p.ReceivedAt.Value >= from
                     && p.ReceivedAt.Value <= to)
            .OrderBy(p => p.ReceivedAt)
            .ToListAsync(ct);

        var revenueLines = salesRaw
            .Select(s => new PnlLineItemDto(
                $"Penjualan — {s.CustomerName}",
                s.CreatedAt,
                s.ReferenceNo,
                s.GrandTotal))
            .ToList();

        var cogsLines = new List<PnlLineItemDto>();
        foreach (var po in posRaw)
        {
            var lines     = JsonSerializer.Deserialize<List<PoLineJson>>(po.LinesJson, _json) ?? [];
            var totalCost = lines.Sum(l => l.ReceivedQty * l.UnitCost);
            if (totalCost <= 0) continue;

            cogsLines.Add(new PnlLineItemDto(
                $"Pembelian dari {po.SupplierName}",
                po.ReceivedAt!.Value,
                po.Id.ToString("N")[..8].ToUpperInvariant(),
                totalCost));
        }

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalCogs    = cogsLines.Sum(l => l.Amount);
        var grossProfit  = totalRevenue - totalCogs;
        var marginPct    = totalRevenue > 0
                           ? Math.Round(grossProfit / totalRevenue * 100, 1)
                           : 0m;

        return new ProfitAndLossDto(
            from, to,
            totalRevenue, totalCogs, grossProfit, marginPct,
            revenueLines.AsReadOnly(), cogsLines.AsReadOnly());
    }

    // ── Cash Flow ─────────────────────────────────────────────────────────────

    public async Task<CashFlowDto> GetCashFlowAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Inflows — completed sales: use PaidAt when available, else CreatedAt
        var salesRaw = await context.SaleReadModels
            .AsNoTracking()
            .Where(s => s.Status != SaleStatus.Draft
                     && s.Status != SaleStatus.Cancelled)
            .Where(s => (s.PaidAt.HasValue  && s.PaidAt.Value  >= from && s.PaidAt.Value  <= to)
                     || (!s.PaidAt.HasValue && s.CreatedAt     >= from && s.CreatedAt     <= to))
            .OrderBy(s => s.PaidAt ?? s.CreatedAt)
            .Select(s => new
            {
                s.ReferenceNo, s.CustomerName,
                EffectiveDate = s.PaidAt ?? s.CreatedAt,
                Amount        = s.PaidAmount ?? s.GrandTotal,
            })
            .ToListAsync(ct);

        // Outflows — received purchase orders (cash paid to suppliers)
        var posRaw = await context.PurchaseOrderReadModels
            .AsNoTracking()
            .Where(p => p.Status == "Received"
                     && p.ReceivedAt.HasValue
                     && p.ReceivedAt.Value >= from
                     && p.ReceivedAt.Value <= to)
            .OrderBy(p => p.ReceivedAt)
            .ToListAsync(ct);

        var inflows = salesRaw
            .Select(s => new CashFlowLineDto(
                $"Penerimaan Penjualan — {s.CustomerName}",
                s.EffectiveDate,
                s.ReferenceNo,
                s.Amount,
                "Penjualan"))
            .ToList();

        var outflows = new List<CashFlowLineDto>();
        foreach (var po in posRaw)
        {
            var lines     = JsonSerializer.Deserialize<List<PoLineJson>>(po.LinesJson, _json) ?? [];
            var totalCost = lines.Sum(l => l.ReceivedQty * l.UnitCost);
            if (totalCost <= 0) continue;

            outflows.Add(new CashFlowLineDto(
                $"Pembayaran Pembelian — {po.SupplierName}",
                po.ReceivedAt!.Value,
                po.Id.ToString("N")[..8].ToUpperInvariant(),
                totalCost,
                "Pembelian"));
        }

        var totalInflows  = inflows.Sum(i => i.Amount);
        var totalOutflows = outflows.Sum(o => o.Amount);

        return new CashFlowDto(
            from, to,
            totalInflows, totalOutflows, totalInflows - totalOutflows,
            inflows.AsReadOnly(), outflows.AsReadOnly());
    }

    // ── Local deserialisation type ────────────────────────────────────────────
    private sealed record PoLineJson(
        Guid    ProductId,
        decimal OrderedQty,
        decimal ReceivedQty,
        decimal UnitCost,
        string  Unit);
}
