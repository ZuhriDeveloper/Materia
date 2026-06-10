using FluentValidation;

namespace Materia.Application.Commands.Financials.RecordChangeFundDeposit;

public class RecordChangeFundDepositCommandValidator
    : AbstractValidator<RecordChangeFundDepositCommand>
{
    public RecordChangeFundDepositCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah harus lebih dari nol.")
            // Sanity cap — a change-fund float beyond Rp 1 miliar is a typo, not a deposit.
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Jumlah melebihi batas wajar (maks. Rp 1.000.000.000).");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);

        RuleFor(x => x.RecordedBy)
            .NotEmpty().WithMessage("Pencatat wajib diisi.");

        // The idempotency key is the client's double-submit guard; an empty key would
        // defeat de-duplication, so it is required.
        RuleFor(x => x.IdempotencyKey)
            .NotEqual(Guid.Empty).WithMessage("Idempotency-Key wajib diisi.");
    }
}
