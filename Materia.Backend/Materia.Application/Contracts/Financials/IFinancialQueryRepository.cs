namespace Materia.Application.Contracts.Financials;

// ── P&L ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Profit &amp; Loss summary for a date range.
/// <para>
/// Phase 1 formulas (all fields are per-period):
/// <list type="bullet">
///   <item><description><b>GrossSales</b>     = sum(SaleReadModel.GrossSubtotal) — revenue before any discounts.</description></item>
///   <item><description><b>DiscountsGiven</b> = sum(SaleReadModel.LineDiscountTotal + SaleReadModel.Discount) — all discounts (line + order-level).</description></item>
///   <item><description><b>NetSales</b>       = sum(SaleReadModel.GrandTotal - SaleReadModel.Tax) — what customers paid, excluding collected VAT.</description></item>
///   <item><description><b>TotalCogs</b>      = sum(SaleReadModel.TotalCost) — matched line cost snapshotted at finalization.</description></item>
///   <item><description><b>GrossProfit</b>    = NetSales - TotalCogs.</description></item>
///   <item><description><b>MarginPct</b>      = GrossProfit / NetSales × 100 (0 when NetSales = 0).</description></item>
///   <item><description><b>TotalRevenue</b>   = NetSales (back-compat alias, previously = sum(GrandTotal)).</description></item>
/// </list>
/// Note: VAT (Tax) is excluded from NetSales because it is collected on behalf of the government
/// and not true operating revenue.
/// </para>
/// </summary>
public record ProfitAndLossDto(
    DateTime                      From,
    DateTime                      To,
    /// <summary>Back-compat alias for <see cref="NetSales"/>.</summary>
    decimal                       TotalRevenue,
    decimal                       TotalCogs,
    decimal                       GrossProfit,
    decimal                       GrossProfitMarginPct,
    IReadOnlyList<PnlLineItemDto> RevenueLines,
    IReadOnlyList<PnlLineItemDto> CogsLines,
    /// <summary>Sum of gross line subtotals before discounts.</summary>
    decimal                       GrossSales        = 0m,
    /// <summary>Sum of all line discounts and order-level discounts given.</summary>
    decimal                       DiscountsGiven    = 0m,
    /// <summary>Net revenue (GrandTotal − Tax) — excludes VAT collected on behalf of government.</summary>
    decimal                       NetSales          = 0m);

public record PnlLineItemDto(
    string   Description,
    DateTime Date,
    string   ReferenceNo,
    decimal  Amount);

// ── Cash Flow ─────────────────────────────────────────────────────────────────

public record CashFlowDto(
    DateTime                         From,
    DateTime                         To,
    decimal                          TotalInflows,
    decimal                          TotalOutflows,
    decimal                          NetCashFlow,
    IReadOnlyList<CashFlowLineDto>   Inflows,
    IReadOnlyList<CashFlowLineDto>   Outflows);

public record CashFlowLineDto(
    string   Description,
    DateTime Date,
    string   ReferenceNo,
    decimal  Amount,
    string   Category);

// ── Repository interface ──────────────────────────────────────────────────────

public interface IFinancialQueryRepository
{
    Task<ProfitAndLossDto> GetProfitAndLossAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    Task<CashFlowDto> GetCashFlowAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
}
