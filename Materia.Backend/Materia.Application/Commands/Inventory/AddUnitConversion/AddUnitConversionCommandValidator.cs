using FluentValidation;
using Materia.Application.Contracts.Inventory;

namespace Materia.Application.Commands.Inventory.AddUnitConversion;

public class AddUnitConversionCommandValidator : AbstractValidator<AddUnitConversionCommand>
{
    public AddUnitConversionCommandValidator(IProductQueryRepository productQueryRepository)
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.ToUnit)
            .NotEmpty()
            .MaximumLength(50)
            .MustAsync(async (cmd, toUnit, ct) =>
            {
                var product = await productQueryRepository.GetByIdAsync(cmd.ProductId, ct);
                if (product is null) return true; // let the handler deal with missing product
                // duplicate if it matches the base unit or an existing conversion
                return !product.BaseUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase)
                    && product.UnitConversions.All(c =>
                        !c.ToUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("Satuan sudah digunakan pada produk ini.");

        RuleFor(x => x.Factor)
            .GreaterThan(0)
            .WithMessage("Faktor konversi harus lebih dari nol.");

        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);

        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
