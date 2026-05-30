using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class StockQueryRepository(AppDbContext context) : IStockQueryRepository
{
    public async Task<StockDto?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        var s = await context.StockReadModels
            .FirstOrDefaultAsync(x => x.ProductId == productId, ct);

        return s is null ? null : new StockDto(
            s.ProductId, s.Quantity, s.Unit, s.LastAdjustedAt, s.LastAdjustedBy);
    }
}
