namespace Materia.Application.Commands.Financials.RecordChangeFundDeposit;

public record RecordChangeFundDepositCommand(
    decimal  Amount,
    string?  Notes,
    string   RecordedBy,
    Guid     IdempotencyKey);
