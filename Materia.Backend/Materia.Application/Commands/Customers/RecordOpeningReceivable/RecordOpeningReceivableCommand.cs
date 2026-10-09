namespace Materia.Application.Commands.Customers.RecordOpeningReceivable;

/// <summary>
/// Records a pre-existing customer debt (saldo awal piutang) that has no sale behind it.
/// <paramref name="ReceivableId"/> is generated once per entry by the client so a
/// double-submitted form is rejected instead of recorded twice.
/// </summary>
public record RecordOpeningReceivableCommand(
    Guid     CustomerId,
    Guid     ReceivableId,
    decimal  Amount,
    DateTime IncurredAt,
    string?  ReferenceNo,
    string?  Notes,
    string   RecordedBy);
