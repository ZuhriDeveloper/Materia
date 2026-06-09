using Materia.Domain.Common;

namespace Materia.Domain.Financials.Events;

/// <summary>
/// Raised when a petty cash (kas kecil) expense is recorded — money paid out of the
/// cash drawer for something outside normal purchasing (fuel, vehicle parts, etc.).
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
    DateTime           OccurredAt) : IDomainEvent;
