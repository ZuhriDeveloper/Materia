using FluentValidation;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.RecordOpeningReceivable;

public class RecordOpeningReceivableCommandValidator
    : AbstractValidator<RecordOpeningReceivableCommand>
{
    public RecordOpeningReceivableCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty).WithMessage("CustomerId tidak valid.");

        RuleFor(x => x.ReceivableId)
            .NotEqual(Guid.Empty).WithMessage("ReceivableId tidak valid.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah piutang harus lebih dari nol.")
            .LessThanOrEqualTo(Customer.MaxOpeningReceivableAmount)
            .WithMessage("Jumlah piutang melebihi batas maksimum (Rp 10 miliar).")
            .Must(a => a == decimal.Truncate(a))
            .WithMessage("Jumlah piutang harus dalam rupiah bulat.");

        RuleFor(x => x.IncurredAt)
            .NotEqual(default(DateTime)).WithMessage("Tanggal piutang wajib diisi.")
            .Must(d => d.Date >= Customer.MinOpeningReceivableDate)
            .WithMessage("Tanggal piutang terlalu lama.")
            .Must(d => d.Date <= Customer.MaxOpeningReceivableDate())
            .WithMessage("Tanggal piutang tidak boleh di masa depan.");

        RuleFor(x => x.ReferenceNo)
            .MaximumLength(50).WithMessage("No. referensi tidak boleh melebihi 50 karakter.")
            .When(x => x.ReferenceNo is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Catatan tidak boleh melebihi 500 karakter.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.RecordedBy)
            .NotEmpty().WithMessage("RecordedBy wajib diisi.");
    }
}
