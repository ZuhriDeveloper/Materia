using FluentValidation;
using Materia.Application.Contracts.Customers;

namespace Materia.Application.Commands.Customers.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator(ICustomerQueryRepository repo)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .NotEmpty().MaximumLength(20)
            .MustAsync(async (phone, ct) => !await repo.ExistsByPhoneAsync(phone, null, ct))
            .WithMessage("Nomor telepon sudah terdaftar.");
        RuleFor(x => x.Email)
            .EmailAddress().MaximumLength(200)
            .When(x => x.Email is not null);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }
}
