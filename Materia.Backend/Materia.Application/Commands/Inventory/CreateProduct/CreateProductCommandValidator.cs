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

        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);

        When(x => !string.IsNullOrWhiteSpace(x.Barcode), () =>
        {
            RuleFor(x => x.Barcode!)
                .MaximumLength(100)
                .MustAsync(async (barcode, ct) =>
                    !await productQueryRepository.ExistsByBarcodeAsync(barcode.Trim(), null, ct))
                .WithMessage("Barcode sudah digunakan produk lain.");
        });
    }
}
