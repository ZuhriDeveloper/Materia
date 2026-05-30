using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IProductQueryRepository productQueryRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync(async (name, ct) => !await productQueryRepository.ExistsByNameAsync(name, null, ct))
            .WithMessage("Produk sudah terdaftar.");

        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.BaseUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}
