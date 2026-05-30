using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Inventory;

public class CategoryQueryRepository(AppDbContext context) : ICategoryQueryRepository
{
    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await context.CategoryReadModels.FindAsync([id], ct);
        return c is null ? null : Map(c);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await context.CategoryReadModels.OrderBy(c => c.Name).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    private static CategoryDto Map(Persistence.Projections.CategoryReadModel c) =>
        new(c.Id, c.Name, c.Description, c.IsActive, c.CreatedBy, c.CreatedAt, c.UpdatedBy, c.UpdatedAt);
}
