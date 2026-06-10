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
        // Revenue + matched COGS — all completed sales in the period.
        // Phase 1: COGS = sum of snapshotted line costs stored on the sale read model.
        //
        // Formula summary (see ProfitAndLossDto XML doc for full definitions):
        //   GrossSales     = sum(SaleReadModel.GrossSubtotal)              [pre-discount revenue]
        //   DiscountsGiven = sum(LineDiscountTotal + Discount)              [all discounts]
        //   NetSales       = sum(GrandTotal - Tax)                         [customer-paid excl. VAT]
        //   TotalCogs      = sum(TotalCost)                                [matched line cost]
        //   GrossProfit    = NetSales - TotalCogs
        //   MarginPct      = GrossProfit / NetSales * 100  (0 when NetSales = 0)
        //   TotalRevenue   = NetSales                      [back-compat alias]
        var salesRaw = await context.SaleReadModels
            .AsNoTracking()
            .Where(s => s.CreatedAt >= from && s.CreatedAt <= to
                     && s.Status != SaleStatus.Draft
                     && s.Status != SaleStatus.Cancelled)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                s.ReferenceNo,
                s.CustomerName,
                s.CreatedAt,
                s.GrandTotal,
                s.Tax,
                s.Discount,
                s.GrossSubtotal,
                s.LineDiscountTotal,
                s.TotalCost,
            })
            .ToListAsync(ct);

        // Revenue lines: one per sale, Amount = GrandTotal − Tax (net revenue excl. VAT)
        var revenueLines = salesRaw
            .Select(s => new PnlLineItemDto(
                $"Penjualan — {s.CustomerName}",
                s.CreatedAt,
                s.ReferenceNo,
                s.GrandTotal - s.Tax))   // net of VAT
            .ToList();

        // COGS lines: one per sale (matched line cost)
        var cogsLines = salesRaw
            .Where(s => s.TotalCost > 0)
            .Select(s => new PnlLineItemDto(
                $"HPP Penjualan — {s.CustomerName}",
                s.CreatedAt,
                s.ReferenceNo,
                s.TotalCost))
            .ToList();

        var grossSales     = salesRaw.Sum(s => s.GrossSubtotal);
        var discountsGiven = salesRaw.Sum(s => s.LineDiscountTotal + s.Discount);
        var netSales       = salesRaw.Sum(s => s.GrandTotal - s.Tax);
        var totalCogs      = salesRaw.Sum(s => s.TotalCost);
        var grossProfit    = netSales - totalCogs;
        var marginPct      = netSales > 0
                             ? Math.Round(grossProfit / netSales * 100, 1)
                             : 0m;

        return new ProfitAndLossDto(
            from, to,
            TotalRevenue:         netSales,       // back-compat field = NetSales
            TotalCogs:            totalCogs,
            GrossProfit:          grossProfit,
            GrossProfitMarginPct: marginPct,
            RevenueLines:         revenueLines.AsReadOnly(),
            CogsLines:            cogsLines.AsReadOnly(),
            GrossSales:           grossSales,
            DiscountsGiven:       discountsGiven,
            NetSales:             netSales);
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
            var totalCost = lines.Sum(l => l.ReceivedQty * l.UnitCost);
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
