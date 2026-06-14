using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Queries.Inventory;

/// <summary>
/// Exports every product matching the given filters (no paging) as an .xlsx workbook.
/// Mirrors <see cref="GetProductsQuery"/>'s filters minus the page/pageSize.
/// </summary>
public record ExportProductsQuery(
    bool? IsActive = null, string? Search = null, Guid? CategoryId = null);

public class ExportProductsQueryHandler(
    IProductQueryRepository repository,
    IProductExcelExporter exporter)
{
    public async Task<byte[]> HandleAsync(ExportProductsQuery query, CancellationToken ct = default)
    {
        var products = await repository.GetAllAsync(
            query.IsActive, query.Search, query.CategoryId, ct);

        var rows = products.Select(ProductExportRowMapper.ToRow).ToList();
        return exporter.Build(rows);
    }
}
