namespace Materia.Domain.Financials;

/// <summary>
/// Where the change fund money came from.
/// IMPORTANT: values are persisted as integers in the append-only event store.
/// New entries may only be appended — never reorder or remove existing members.
/// </summary>
public enum ChangeFundSource
{
    /// <summary>Cash deposited manually by the admin.</summary>
    ManualEntry = 0,

    /// <summary>Exchanged from petty cash (kas kecil) — a PettyCashExpense was recorded first.</summary>
    PettyCashExchange = 1,
}
