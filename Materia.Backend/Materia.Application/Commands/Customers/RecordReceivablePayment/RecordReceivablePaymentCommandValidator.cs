using FluentValidation;
using Materia.Domain.Sales;

namespace Materia.Application.Commands.Customers.RecordReceivablePayment;

public class RecordReceivablePaymentCommandValidator
    : AbstractValidator<RecordReceivablePaymentCommand>
{
    public RecordReceivablePaymentCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty).WithMessage("CustomerId tidak valid.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah pembayaran harus lebih dari nol.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("Metode pembayaran tidak valid.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan tidak boleh melebihi 500 karakter.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.ReceivedBy)
            .NotEmpty().WithMessage("ReceivedBy wajib diisi.");

        // The idempotency key is the client's double-submit guard; an empty key would
        // defeat de-duplication, so it is required.
        RuleFor(x => x.IdempotencyKey)
            .NotEqual(Guid.Empty).WithMessage("Idempotency-Key wajib diisi.");
    }
}
