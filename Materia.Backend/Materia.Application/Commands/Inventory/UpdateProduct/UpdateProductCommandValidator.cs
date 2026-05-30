using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IProductQueryRepository productQueryRepository)
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync(async (cmd, name, ct) =>
                !await productQueryRepository.ExistsByNameAsync(name, cmd.ProductId, ct))
            .WithMessage("Produk sudah terdaftar.");

        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
