namespace Materia.Domain.Financials;

/// <summary>
/// Predefined reason templates for a petty cash (kas kecil) expense.
/// <para>
/// IMPORTANT: values are persisted as integers in the append-only event store.
/// New categories may only be <b>appended</b> — never reorder or remove existing
/// members, or historical events will deserialize to the wrong category.
/// </para>
/// </summary>
public enum PettyCashCategory
{
    /// <summary>Bensin (fuel).</summary>
    Bensin = 0,

    /// <summary>Sparepart Kendaraan (vehicle spare parts).</summary>
    SparepartKendaraan = 1,

    /// <summary>Belanja lainnya (other shopping).</summary>
    BelanjaLainnya = 2,

    /// <summary>Lainnya (other) — requires a free-text "Sebutkan" detail.</summary>
    Lainnya = 3,
}
