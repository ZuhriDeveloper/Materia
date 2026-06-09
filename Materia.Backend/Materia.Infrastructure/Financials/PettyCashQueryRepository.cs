using Materia.Application.Contracts.Financials;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Financials;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Financials;

public class PettyCashQueryRepository(AppDbContext context) : IPettyCashQueryRepository
{
    public async Task<PagedResult<PettyCashExpenseDto>> GetPagedAsync(
        int page, int pageSize, DateTime? from, DateTime? to,
        PettyCashCategory? category, CancellationToken ct = default)
    {
        page     = page     < 1 ? 1  : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var query = context.PettyCashExpenseReadModels
            .AsNoTracking()
            .Where(e => !e.IsVoided);

        if (from.HasValue)     query = query.Where(e => e.RecordedAt >= from.Value);
        if (to.HasValue)       query = query.Where(e => e.RecordedAt <= to.Value);
        if (category.HasValue) query = query.Where(e => e.Category == category.Value);

        var totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(e => e.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(MapToDto).ToList();
        return new PagedResult<PettyCashExpenseDto>(items, totalCount, page, pageSize);
    }

    public async Task<PettyCashExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var row = await context.PettyCashExpenseReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return row is null ? null : MapToDto(row);
    }

    private static PettyCashExpenseDto MapToDto(Persistence.Projections.PettyCashExpenseReadModel row) =>
        new(row.Id, row.Amount, row.Recipient, row.Category, row.ReasonDetail,
            row.ReasonText, row.Notes, row.ReferenceNo, row.RecordedBy, row.RecordedAt);
}
