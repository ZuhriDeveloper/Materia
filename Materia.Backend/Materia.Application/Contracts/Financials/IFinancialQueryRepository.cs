namespace Materia.Application.Contracts.Financials;

// ── P&L ───────────────────────────────────────────────────────────────────────

public record ProfitAndLossDto(
    DateTime                      From,
    DateTime                      To,
    decimal                       TotalRevenue,
    decimal                       TotalReturns,
    decimal                       TotalCogs,
    decimal                       GrossProfit,
    decimal                       GrossProfitMarginPct,
    IReadOnlyList<PnlLineItemDto> RevenueLines,
    IReadOnlyList<PnlLineItemDto> ReturnLines,
    IReadOnlyList<PnlLineItemDto> CogsLines);

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
