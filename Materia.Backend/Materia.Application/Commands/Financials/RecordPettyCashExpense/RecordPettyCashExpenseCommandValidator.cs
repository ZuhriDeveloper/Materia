using FluentValidation;
using Materia.Domain.Financials;

namespace Materia.Application.Commands.Financials.RecordPettyCashExpense;

public class RecordPettyCashExpenseCommandValidator : AbstractValidator<RecordPettyCashExpenseCommand>
{
    public RecordPettyCashExpenseCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah pengeluaran harus lebih dari nol.");

        RuleFor(x => x.Recipient)
            .NotEmpty().WithMessage("Penerima wajib diisi.")
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Kategori tidak valid.");

        // "Sebutkan" is mandatory only for the free-text "Lainnya" template.
        RuleFor(x => x.ReasonDetail)
            .NotEmpty().WithMessage("Sebutkan alasan untuk kategori 'Lainnya'.")
            .MaximumLength(300)
            .When(x => x.Category == PettyCashCategory.Lainnya);

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);

        RuleFor(x => x.RecordedBy).NotEmpty();
    }
}
