namespace Materia.Domain.Financials;

public static class PettyCashCategoryExtensions
{
    /// <summary>Human-readable Indonesian label for a category template.</summary>
    public static string ToDisplayLabel(this PettyCashCategory category) => category switch
    {
        PettyCashCategory.Bensin             => "Bensin",
        PettyCashCategory.SparepartKendaraan => "Sparepart Kendaraan",
        PettyCashCategory.BelanjaLainnya     => "Belanja lainnya",
        PettyCashCategory.Lainnya            => "Lainnya",
        _                                    => category.ToString(),
    };
}
