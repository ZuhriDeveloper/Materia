using Materia.Application.Contracts.Customers;
using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Customers;

public record GetCustomersQuery(int Page, int PageSize, string? Search, bool? IsActive);

public record GetCustomerByIdQuery(Guid Id);

public class GetCustomersQueryHandler(ICustomerQueryRepository repository)
{
    public Task<PagedResult<CustomerDto>> HandleAsync(
        GetCustomersQuery query, CancellationToken ct = default)
        => repository.GetPagedAsync(query.Page, query.PageSize, query.Search, query.IsActive, ct);

    public Task<CustomerDto?> HandleByIdAsync(
        GetCustomerByIdQuery query, CancellationToken ct = default)
        => repository.GetByIdAsync(query.Id, ct);
}
