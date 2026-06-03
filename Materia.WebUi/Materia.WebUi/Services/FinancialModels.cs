namespace Materia.WebUi.Services;

// ── Profit & Loss ─────────────────────────────────────────────────────────────

public record ProfitAndLossResult(
    DateTime                   From,
    DateTime                   To,
    decimal                    TotalRevenue,
    decimal                    TotalCogs,
    decimal                    GrossProfit,
    decimal                    GrossProfitMarginPct,
    List<PnlLineItemResult>    RevenueLines,
    List<PnlLineItemResult>    CogsLines);

public record PnlLineItemResult(
    string   Description,
    DateTime Date,
    string   ReferenceNo,
    decimal  Amount);

// ── Cash Flow ─────────────────────────────────────────────────────────────────

public record CashFlowResult(
    DateTime                    From,
    DateTime                    To,
    decimal                     TotalInflows,
    decimal                     TotalOutflows,
    decimal                     NetCashFlow,
    List<CashFlowLineResult>    Inflows,
    List<CashFlowLineResult>    Outflows);

public record CashFlowLineResult(
    string   Description,
    DateTime Date,
    string   ReferenceNo,
    decimal  Amount,
    string   Category);
