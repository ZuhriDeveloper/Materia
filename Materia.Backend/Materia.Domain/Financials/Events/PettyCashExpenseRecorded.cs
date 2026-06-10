using Materia.Domain.Common;

namespace Materia.Domain.Financials.Events;

/// <summary>
/// Raised when a petty cash (kas kecil) expense is recorded — money paid out of the
/// cash drawer for something outside normal purchasing (fuel, vehicle parts, etc.).
/// <paramref name="IdempotencyKey"/> is the client-supplied de-duplication token, baked
/// in so the projection's unique index can reject duplicate submissions. Events stored
/// before the key existed deserialize to <see cref="Guid.Empty"/>, which is fine — the
/// key is only consulted when writing new records.
/// </summary>
public record PettyCashExpenseRecorded(
    PettyCashExpenseId Id,
    decimal            Amount,
    string             Recipient,
    PettyCashCategory  Category,
    string?            ReasonDetail,
    string?            Notes,
    string             ReferenceNo,
    string             RecordedBy,
    DateTime           OccurredAt,
    Guid               IdempotencyKey = default) : IDomainEvent;
