using Materia.Application.Contracts.Inventory;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Inventory;

public record GetProductByIdQuery(Guid Id);

public class GetProductByIdQueryHandler(IProductQueryRepository repository)
{
    public Task<ProductDto?> HandleAsync(GetProductByIdQuery query, CancellationToken ct = default)
        => repository.GetByIdAsync(query.Id, ct);
}
