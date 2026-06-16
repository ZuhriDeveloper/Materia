using FluentValidation;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.DTOs.Inventory;

namespace Materia.Application.Queries.Purchasing;

public record GetSuppliersPagedQuery(
    string? Search   = null,
    bool    ActiveOnly = false,
    int     Page     = 1,
    int     PageSize = 20);

public sealed class GetSuppliersPagedQueryHandler(ISupplierQueryRepository repository)
{
    public Task<PagedResult<SupplierDto>> HandleAsync(
        GetSuppliersPagedQuery query, CancellationToken ct = default)
        => repository.SearchAsync(query.Search, query.ActiveOnly, query.Page, query.PageSize, ct);
}

public sealed class GetSuppliersPagedQueryValidator : AbstractValidator<GetSuppliersPagedQuery>
{
    public GetSuppliersPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
