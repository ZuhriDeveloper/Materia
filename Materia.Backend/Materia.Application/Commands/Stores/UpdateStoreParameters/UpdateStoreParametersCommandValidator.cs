using FluentValidation;

namespace Materia.Application.Commands.Stores.UpdateStoreParameters;

public class UpdateStoreParametersCommandValidator : AbstractValidator<UpdateStoreParametersCommand>
{
    public UpdateStoreParametersCommandValidator()
    {
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MaxDeliveryDistanceKm).GreaterThan(0);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
