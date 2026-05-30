using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetUnitsQuery;

public class GetUnitsQueryHandler(IUnitQueryRepository repository)
{
    public Task<IReadOnlyList<UnitDto>> HandleAsync(GetUnitsQuery query, CancellationToken ct = default)
        => repository.GetAllAsync(ct);
}
