using FluentValidation;

namespace Materia.Application.Queries.Inventory;

public class SemanticProductSearchQueryValidator : AbstractValidator<SemanticProductSearchQuery>
{
    public SemanticProductSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query tidak boleh kosong.")
            .MaximumLength(500).WithMessage("Query terlalu panjang (maksimal 500 karakter).");

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 20).WithMessage("TopK harus antara 1 dan 20.");

        RuleFor(x => x.MinScore)
            .InclusiveBetween(0d, 1d).WithMessage("MinScore harus antara 0 dan 1.");
    }
}
