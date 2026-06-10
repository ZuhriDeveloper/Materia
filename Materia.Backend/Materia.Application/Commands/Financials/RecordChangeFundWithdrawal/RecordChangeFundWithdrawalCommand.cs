namespace Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;

public record RecordChangeFundWithdrawalCommand(
    decimal Amount,
    string  Reason,
    string  RecordedBy,
    Guid    IdempotencyKey);
