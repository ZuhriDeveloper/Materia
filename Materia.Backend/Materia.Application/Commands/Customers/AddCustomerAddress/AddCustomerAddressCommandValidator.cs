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
        RuleFor(x => x.Subdistrict).MaximumLength(100).When(x => x.Subdistrict is not null);
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        // Coordinates are optional (the map pin). Validate the range only when provided.
        RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude harus antara -90 dan 90.");
        RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude harus antara -180 dan 180.");
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
