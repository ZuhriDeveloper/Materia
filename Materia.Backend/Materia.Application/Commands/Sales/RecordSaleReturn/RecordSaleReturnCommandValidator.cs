using FluentValidation;

namespace Materia.Application.Commands.Sales.RecordSaleReturn;

public sealed class RecordSaleReturnCommandValidator : AbstractValidator<RecordSaleReturnCommand>
{
    private static readonly string[] ValidResolutions = ["CashRefund", "DebtReduction"];

    public RecordSaleReturnCommandValidator()
    {
        RuleFor(x => x.OriginalSaleId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Retur harus memiliki minimal satu item.");
        RuleFor(x => x.Resolution)
            .Must(r => ValidResolutions.Contains(r))
            .WithMessage("Resolusi tidak valid. Pilih 'CashRefund' atau 'DebtReduction'.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ReturnedBy).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitName).NotEmpty();
        });
    }
}
