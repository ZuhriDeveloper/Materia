using FluentValidation;

namespace Materia.Application.Commands.Purchasing.RegisterSupplier;

public sealed class RegisterSupplierCommandValidator : AbstractValidator<RegisterSupplierCommand>
{
    public RegisterSupplierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPhone).MaximumLength(50).When(x => x.ContactPhone is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.SalesmanName).MaximumLength(200).When(x => x.SalesmanName is not null);
        RuleFor(x => x.SalesmanPhone).MaximumLength(50).When(x => x.SalesmanPhone is not null);
        RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(100);
    }
}
