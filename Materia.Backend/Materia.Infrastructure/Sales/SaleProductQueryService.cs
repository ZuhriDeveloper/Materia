using Materia.Application.Contracts.Sales;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Sales;

public class SaleProductQueryService(AppDbContext context) : ISaleProductQueryService
{
    public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken ct = default)
        => context.SaleItemReadModels
            .AnyAsync(i => i.ProductId == productId, ct);
}
