namespace Materia.WebUi.Services;

/// <summary>
/// Reason templates for a petty cash (kas kecil) entry. Mirrors the API enum
/// (Materia.Domain.Financials.PettyCashCategory) — keep the integer values in sync.
/// </summary>
public enum PettyCashCategory
{
    Bensin             = 0,
    SparepartKendaraan = 1,
    BelanjaLainnya     = 2,
    Lainnya            = 3,
}

public static class PettyCashCategoryLabels
{
    public static string Label(this PettyCashCategory category) => category switch
    {
        PettyCashCategory.Bensin             => "Bensin",
        PettyCashCategory.SparepartKendaraan => "Sparepart Kendaraan",
        PettyCashCategory.BelanjaLainnya     => "Belanja lainnya",
        PettyCashCategory.Lainnya            => "Lainnya",
        _                                    => category.ToString(),
    };

    public static IReadOnlyList<PettyCashCategory> All { get; } =
        [PettyCashCategory.Bensin, PettyCashCategory.SparepartKendaraan,
         PettyCashCategory.BelanjaLainnya, PettyCashCategory.Lainnya];
}

public record PettyCashExpenseResult(
    Guid              Id,
    decimal           Amount,
    string            Recipient,
    PettyCashCategory Category,
    string?           ReasonDetail,
    string            Reason,
    string?           Notes,
    string            ReferenceNo,
    string            RecordedBy,
    DateTime          RecordedAt);

public record PagedPettyCashResult(
    List<PettyCashExpenseResult> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
