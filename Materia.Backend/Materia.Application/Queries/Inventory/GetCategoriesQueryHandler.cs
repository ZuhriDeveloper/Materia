using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetCategoriesQuery;

public record GetCategoryByIdQuery(Guid Id);

public class GetCategoriesQueryHandler(ICategoryQueryRepository repository)
{
    public Task<IReadOnlyList<CategoryDto>> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
        => repository.GetAllAsync(ct);

    public Task<CategoryDto?> HandleByIdAsync(GetCategoryByIdQuery query, CancellationToken ct = default)
        => repository.GetByIdAsync(query.Id, ct);
}
