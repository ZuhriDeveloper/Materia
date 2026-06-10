using FluentValidation;

namespace Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;

public class RecordChangeFundWithdrawalCommandValidator
    : AbstractValidator<RecordChangeFundWithdrawalCommand>
{
    public RecordChangeFundWithdrawalCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah harus lebih dari nol.")
            // Sanity cap — same bound as deposits; anything beyond Rp 1 miliar is a typo.
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Jumlah melebihi batas wajar (maks. Rp 1.000.000.000).");

        // The reason is the audit trail for a correction — never optional.
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Alasan penarikan wajib diisi.")
            .MaximumLength(300);

        RuleFor(x => x.RecordedBy)
            .NotEmpty().WithMessage("Pencatat wajib diisi.");

        // The idempotency key is the client's double-submit guard; an empty key would
        // defeat de-duplication, so it is required.
        RuleFor(x => x.IdempotencyKey)
            .NotEqual(Guid.Empty).WithMessage("Idempotency-Key wajib diisi.");
    }
}
