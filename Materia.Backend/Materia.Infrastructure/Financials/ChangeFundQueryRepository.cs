using Materia.Application.Contracts.Financials;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Financials;

public class ChangeFundQueryRepository(AppDbContext context) : IChangeFundQueryRepository
{
    public async Task<PagedResult<ChangeFundDepositDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page     = page     < 1 ? 1  : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = context.ChangeFundDepositReadModels.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(e => e.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(MapToDto).ToList();
        return new PagedResult<ChangeFundDepositDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<ChangeFundWithdrawalDto>> GetPagedWithdrawalsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page     = page     < 1 ? 1  : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = context.ChangeFundWithdrawalReadModels.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(e => e.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ChangeFundWithdrawalDto(
                r.Id, r.Amount, r.Reason, r.RecordedBy, r.RecordedAt))
            .ToList();
        return new PagedResult<ChangeFundWithdrawalDto>(items, totalCount, page, pageSize);
    }

    public async Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default)
    {
        var deposits = await context.ChangeFundDepositReadModels
            .AsNoTracking()
            .SumAsync(e => e.Amount, ct);

        var withdrawals = await context.ChangeFundWithdrawalReadModels
            .AsNoTracking()
            .SumAsync(e => e.Amount, ct);

        return deposits - withdrawals;
    }

    private static ChangeFundDepositDto MapToDto(
        Persistence.Projections.ChangeFundDepositReadModel row) =>
        new(row.Id,
            row.Amount,
            row.Source.ToString(),
            row.SourceReferenceNo,
            row.Notes,
            row.RecordedBy,
            row.RecordedAt);
}
