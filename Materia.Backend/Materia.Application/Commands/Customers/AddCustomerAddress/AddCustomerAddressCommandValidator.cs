using FluentValidation;

namespace Materia.Application.Commands.Customers.AddCustomerAddress;

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(500);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(10).When(x => x.PostalCode is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude harus antara -90 dan 90.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude harus antara -180 dan 180.");
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
