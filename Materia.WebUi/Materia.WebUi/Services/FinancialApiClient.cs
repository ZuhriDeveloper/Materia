using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public sealed class FinancialApiClient(HttpClient http)
{
    public Task<ProfitAndLossResult?> GetProfitAndLossAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => http.GetFromJsonAsync<ProfitAndLossResult>(
            $"api/financials/pnl?from={from:O}&to={to:O}", ct);

    public Task<CashFlowResult?> GetCashFlowAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => http.GetFromJsonAsync<CashFlowResult>(
            $"api/financials/cashflow?from={from:O}&to={to:O}", ct);
}
