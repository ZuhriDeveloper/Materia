using Materia.Application.Queries.Inventory;

namespace Materia.Application.Contracts.Inventory;

/// <summary>
/// Renders product export rows into an .xlsx workbook. Implemented in Infrastructure so the
/// Application layer stays free of any spreadsheet library.
/// </summary>
public interface IProductExcelExporter
{
    byte[] Build(IReadOnlyList<ProductExportRow> rows);
}
