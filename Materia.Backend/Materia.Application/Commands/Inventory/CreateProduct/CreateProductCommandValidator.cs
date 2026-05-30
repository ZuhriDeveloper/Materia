using FluentValidation;

namespace Materia.Application.Commands.Inventory.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.BaseUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}
