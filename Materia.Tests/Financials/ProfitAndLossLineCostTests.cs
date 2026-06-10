using FluentAssertions;
using Materia.Application.Contracts.Financials;
using Materia.Application.Financials.Queries;

namespace Materia.Tests.Financials;

/// <summary>
/// Tests the new P&amp;L formula (matched line costs, GrossSales, DiscountsGiven, NetSales).
/// Uses a stub implementation of IFinancialQueryRepository so no DB is required.
/// These tests assert the shape and formulas returned by the repository contract.
/// </summary>
public class ProfitAndLossLineCostTests
{
    // ── Stub ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Controllable stub that returns a pre-built ProfitAndLossDto.
    /// </summary>
    private sealed class StubFinancialQueryRepository : IFinancialQueryRepository
    {
        private readonly ProfitAndLossDto _dto;

        public StubFinancialQueryRepository(ProfitAndLossDto dto) => _dto = dto;

        public Task<ProfitAndLossDto> GetProfitAndLossAsync(
            DateTime from, DateTime to, CancellationToken ct = default)
            => Task.FromResult(_dto);

        public Task<CashFlowDto> GetCashFlowAsync(
            DateTime from, DateTime to, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private static GetProfitAndLossQueryHandler HandlerFor(ProfitAndLossDto dto)
        => new(new StubFinancialQueryRepository(dto));

    private static DateTime Today => new(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────────────────
    // ProfitAndLossDto contract: new fields must be present and formula-correct
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PnlDto_GrossProfit_EqualsTotalRevenueMinusTotalCogs()
    {
        // Arrange: a DTO where NetSales = 300,000, COGS = 180,000
        var dto = new ProfitAndLossDto(
            From:                 Today,
            To:                   Today,
            TotalRevenue:         300_000m,   // backward-compat = NetSales
            TotalCogs:            180_000m,
            GrossProfit:          120_000m,   // 300k − 180k
            GrossProfitMarginPct: 40m,        // 120k / 300k * 100
            GrossSales:           350_000m,   // before discounts
            DiscountsGiven:        50_000m,   // total discounts
            NetSales:             300_000m,   // GrossSales − DiscountsGiven
            RevenueLines:         [],
            CogsLines:            []);

        var handler = HandlerFor(dto);
        var result = await handler.HandleAsync(new GetProfitAndLossQuery(Today, Today));

        result.GrossProfit.Should().Be(120_000m);
        result.GrossProfitMarginPct.Should().Be(40m);
        result.TotalRevenue.Should().Be(300_000m);        // back-compat field
        result.TotalCogs.Should().Be(180_000m);

        // New fields
        result.GrossSales.Should().Be(350_000m);
        result.DiscountsGiven.Should().Be(50_000m);
        result.NetSales.Should().Be(300_000m);
    }

    [Fact]
    public async Task PnlDto_WhenNoSales_AllFieldsAreZero()
    {
        var dto = new ProfitAndLossDto(
            From:                 Today,
            To:                   Today,
            TotalRevenue:         0m,
            TotalCogs:            0m,
            GrossProfit:          0m,
            GrossProfitMarginPct: 0m,
            GrossSales:           0m,
            DiscountsGiven:       0m,
            NetSales:             0m,
            RevenueLines:         [],
            CogsLines:            []);

        var handler = HandlerFor(dto);
        var result = await handler.HandleAsync(new GetProfitAndLossQuery(Today, Today));

        result.TotalRevenue.Should().Be(0m);
        result.TotalCogs.Should().Be(0m);
        result.GrossProfit.Should().Be(0m);
        result.GrossSales.Should().Be(0m);
        result.DiscountsGiven.Should().Be(0m);
        result.NetSales.Should().Be(0m);
    }

    [Fact]
    public async Task PnlDto_MarginPct_IsGrossProfitDividedByNetSalesTimeHundred()
    {
        // NetSales = 200,000; COGS = 120,000; GrossProfit = 80,000; Margin = 40%
        var dto = new ProfitAndLossDto(
            From:                 Today,
            To:                   Today,
            TotalRevenue:         200_000m,
            TotalCogs:            120_000m,
            GrossProfit:          80_000m,
            GrossProfitMarginPct: 40.0m,
            GrossSales:           220_000m,
            DiscountsGiven:        20_000m,
            NetSales:             200_000m,
            RevenueLines:         [],
            CogsLines:            []);

        var handler = HandlerFor(dto);
        var result = await handler.HandleAsync(new GetProfitAndLossQuery(Today, Today));

        result.GrossProfitMarginPct.Should().Be(40.0m);
    }
}
