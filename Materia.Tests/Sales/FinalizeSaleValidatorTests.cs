using FluentAssertions;
using Materia.Application.Commands.Sales.FinalizeSale;

namespace Materia.Tests.Sales;

public class FinalizeSaleValidatorTests
{
    private readonly FinalizeSaleCommandValidator _validator = new();

    private static FinalizeSaleCommand Valid(decimal? amountPaid = null) =>
        new(
            CustomerId:         null,
            CustomerName:       null,
            Items:              [new FinalizeSaleItemInput(Guid.NewGuid(), "Semen 50kg", "sak", 2m, 75_000m)],
            IsDeliveryRequired: false,
            ServedBy:           "kasir-01",
            AmountPaid:         amountPaid);

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NullAmountPaid_MeansFullPayment_Passes()
    {
        var result = _validator.Validate(Valid(amountPaid: null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativeAmountPaid_Fails()
    {
        var result = _validator.Validate(Valid(amountPaid: -1m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(FinalizeSaleCommand.AmountPaid));
    }

    [Fact]
    public void AmountPaidAboveSanityCap_Fails()
    {
        var result = _validator.Validate(Valid(amountPaid: 1_000_000_001m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(FinalizeSaleCommand.AmountPaid));
    }

    [Fact]
    public void AmountPaidAtSanityCap_Passes()
    {
        var result = _validator.Validate(Valid(amountPaid: 1_000_000_000m));
        result.IsValid.Should().BeTrue();
    }
}
