using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.AddColorVariant;

public class AddColorVariantCommandValidator : AbstractValidator<AddColorVariantCommand>
{
    public AddColorVariantCommandValidator(IProductQueryRepository productQueryRepository)
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.ColorName)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(async (cmd, colorName, ct) =>
            {
                var product = await productQueryRepository.GetByIdAsync(cmd.ProductId, ct);
                if (product is null) return true; // let the handler deal with missing product
                return product.ColorVariants.All(v =>
                    !string.Equals(v.ColorName, colorName, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("Warna ini sudah ada pada produk.");

        RuleFor(x => x.ColorCode).MaximumLength(50);
        RuleFor(x => x.Barcode).MaximumLength(100);

        RuleFor(x => x.PriceOverride)
            .GreaterThanOrEqualTo(0).When(x => x.PriceOverride.HasValue)
            .WithMessage("Harga khusus warna tidak boleh negatif.");

        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
