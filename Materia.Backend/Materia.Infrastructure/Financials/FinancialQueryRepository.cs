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
            .Where(p => (p.Status == "Received" || p.Status == "Closed")
                     && p.ReceivedAt.HasValue
                     && p.ReceivedAt.Value >= from
                     && p.ReceivedAt.Value <= to)
            .OrderBy(p => p.ReceivedAt)
            .ToListAsync(ct);

        // Sales returns in the period — contra-revenue (netted off gross sales).
        // Dated by ReturnedAt: a return reduces revenue in the period it occurs.
        var returnsRaw = await context.SaleReturnReadModels
            .AsNoTracking()
            .Where(r => r.ReturnedAt >= from && r.ReturnedAt <= to)
            .OrderBy(r => r.ReturnedAt)
            .Select(r => new { r.OriginalReferenceNo, r.ReturnedAt, r.TotalRefundAmount })
            .ToListAsync(ct);

        var revenueLines = salesRaw
            .Select(s => new PnlLineItemDto(
                $"Penjualan — {s.CustomerName}",
                s.CreatedAt,
                s.ReferenceNo,
                s.GrandTotal))
            .ToList();

        var returnLines = returnsRaw
            .Select(r => new PnlLineItemDto(
                $"Retur Penjualan — {r.OriginalReferenceNo}",
                r.ReturnedAt,
                r.OriginalReferenceNo,
                r.TotalRefundAmount))
            .ToList();

        var cogsLines = new List<PnlLineItemDto>();
        foreach (var po in posRaw)
        {
            var lines     = JsonSerializer.Deserialize<List<PoLineJson>>(po.LinesJson, _json) ?? [];
            var totalCost = lines.Sum(l => (l.ReceivedQty - l.ReturnedQty) * l.UnitCost);
            if (totalCost <= 0) continue;

            cogsLines.Add(new PnlLineItemDto(
                $"Pembelian dari {po.SupplierName}",
                po.ReceivedAt!.Value,
                po.Id.ToString("N")[..8].ToUpperInvariant(),
                totalCost));
        }

        var totalRevenue = revenueLines.Sum(l => l.Amount);
        var totalReturns = returnLines.Sum(l => l.Amount);
        var totalCogs    = cogsLines.Sum(l => l.Amount);
        var netSales     = totalRevenue - totalReturns;
        var grossProfit  = netSales - totalCogs;
        var marginPct    = netSales > 0
                           ? Math.Round(grossProfit / netSales * 100, 1)
                           : 0m;

        return new ProfitAndLossDto(
            from, to,
            totalRevenue, totalReturns, totalCogs, grossProfit, marginPct,
            revenueLines.AsReadOnly(), returnLines.AsReadOnly(), cogsLines.AsReadOnly());
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
            .Where(p => (p.Status == "Received" || p.Status == "Closed")
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

        // Outflows — petty cash (kas kecil) expenses paid out of the drawer
        var pettyCashRaw = await context.PettyCashExpenseReadModels
            .AsNoTracking()
            .Where(e => !e.IsVoided
                     && e.RecordedAt >= from
                     && e.RecordedAt <= to)
            .OrderBy(e => e.RecordedAt)
            .Select(e => new { e.ReasonText, e.Recipient, e.RecordedAt, e.ReferenceNo, e.Amount })
            .ToListAsync(ct);

        var outflows = new List<CashFlowLineDto>();
        foreach (var po in posRaw)
        {
            var lines     = JsonSerializer.Deserialize<List<PoLineJson>>(po.LinesJson, _json) ?? [];
            var totalCost = lines.Sum(l => (l.ReceivedQty - l.ReturnedQty) * l.UnitCost);
            if (totalCost <= 0) continue;

            outflows.Add(new CashFlowLineDto(
                $"Pembayaran Pembelian — {po.SupplierName}",
                po.ReceivedAt!.Value,
                po.Id.ToString("N")[..8].ToUpperInvariant(),
                totalCost,
                "Pembelian"));
        }

        outflows.AddRange(pettyCashRaw.Select(e => new CashFlowLineDto(
            $"Kas Kecil — {e.ReasonText} ({e.Recipient})",
            e.RecordedAt,
            e.ReferenceNo,
            e.Amount,
            "Kas Kecil")));

        // Outflows — cash refunds from sales returns. Only CashRefund leaves the drawer;
        // DebtReduction returns move no cash (they lower the customer's piutang instead).
        var refundsRaw = await context.SaleReturnReadModels
            .AsNoTracking()
            .Where(r => r.Resolution == ReturnResolution.CashRefund
                     && r.ReturnedAt >= from
                     && r.ReturnedAt <= to)
            .OrderBy(r => r.ReturnedAt)
            .Select(r => new { r.OriginalReferenceNo, r.ReturnedAt, r.TotalRefundAmount })
            .ToListAsync(ct);

        outflows.AddRange(refundsRaw.Select(r => new CashFlowLineDto(
            $"Retur Penjualan — {r.OriginalReferenceNo}",
            r.ReturnedAt,
            r.OriginalReferenceNo,
            r.TotalRefundAmount,
            "Retur Penjualan")));

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
        decimal ReturnedQty,
        decimal UnitCost,
        string  Unit);
}
