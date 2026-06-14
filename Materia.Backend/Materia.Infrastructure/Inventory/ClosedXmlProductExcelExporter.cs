using ClosedXML.Excel;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Queries.Inventory;

namespace Materia.Infrastructure.Inventory;

/// <summary>
/// Builds the products .xlsx export with ClosedXML. Column order mirrors the admin product
/// list; the "Catatan" column carries the master-data gaps from
/// <see cref="ProductExportRow.Notes"/> and is highlighted when non-empty.
/// </summary>
public class ClosedXmlProductExcelExporter : IProductExcelExporter
{
    private static readonly string[] Headers =
    [
        "Nama", "Satuan Dasar", "Stok", "Harga Beli", "Harga Jual",
        "Barcode", "Kategori", "Status", "Diperbarui", "Catatan",
    ];

    private const string MoneyFormat = "#,##0";
    private const string QuantityFormat = "#,##0.##";
    private const string DateFormat = "dd mmm yyyy";

    public byte[] Build(IReadOnlyList<ProductExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Produk");

        // Header row.
        for (var c = 0; c < Headers.Length; c++)
            ws.Cell(1, c + 1).Value = Headers[c];
        var header = ws.Range(1, 1, 1, Headers.Length).Style;
        header.Font.Bold = true;
        header.Fill.BackgroundColor = XLColor.FromHtml("#F1F3F5");

        // Data rows.
        var r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Name;
            ws.Cell(r, 2).Value = row.BaseUnit;

            var stock = ws.Cell(r, 3);
            stock.Value = row.StockQuantity;
            stock.Style.NumberFormat.Format = QuantityFormat;

            var buy = ws.Cell(r, 4);
            if (row.PurchasePrice is { } purchase)
            {
                buy.Value = purchase;
                buy.Style.NumberFormat.Format = MoneyFormat;
            }

            var sell = ws.Cell(r, 5);
            sell.Value = row.SalePrice;
            sell.Style.NumberFormat.Format = MoneyFormat;

            ws.Cell(r, 6).Value = row.Barcode ?? "";
            ws.Cell(r, 7).Value = row.Categories;
            ws.Cell(r, 8).Value = row.IsActive ? "Aktif" : "Nonaktif";

            var updated = ws.Cell(r, 9);
            updated.Value = row.UpdatedAt;
            updated.Style.DateFormat.Format = DateFormat;

            var notes = ws.Cell(r, 10);
            notes.Value = row.Notes;
            if (!string.IsNullOrEmpty(row.Notes))
                notes.Style.Font.FontColor = XLColor.FromHtml("#B45309"); // amber, matches the list's attention cue

            r++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
