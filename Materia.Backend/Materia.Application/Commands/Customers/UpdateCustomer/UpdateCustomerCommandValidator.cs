using FluentValidation;
using Materia.Application.Contracts.Customers;

namespace Materia.Application.Commands.Customers.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator(ICustomerQueryRepository repo)
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .NotEmpty().MaximumLength(20)
            .MustAsync(async (cmd, phone, ct) =>
                !await repo.ExistsByPhoneAsync(phone, cmd.CustomerId, ct))
            .WithMessage("Nomor telepon sudah terdaftar.");
        RuleFor(x => x.Email)
            .EmailAddress().MaximumLength(200)
            .When(x => x.Email is not null);
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
