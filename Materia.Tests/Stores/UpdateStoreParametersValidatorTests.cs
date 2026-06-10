using FluentAssertions;
using Materia.Application.Commands.Stores.UpdateStoreParameters;

namespace Materia.Tests.Stores;

public class UpdateStoreParametersValidatorTests
{
    private readonly UpdateStoreParametersCommandValidator _validator = new();

    private static UpdateStoreParametersCommand Valid(
        string address = "Jl. Merdeka 1",
        string phone = "+628123456789",
        decimal maxDeliveryDistanceKm = 10m,
        string updatedBy = "admin-1") =>
        new(address, phone, maxDeliveryDistanceKm, updatedBy);

    [Fact]
    public void Valid_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankAddress_Fails(string address)
    {
        var result = _validator.Validate(Valid(address: address));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.Address));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankPhone_Fails(string phone)
    {
        var result = _validator.Validate(Valid(phone: phone));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.Phone));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveDistance_Fails(double distance)
    {
        var result = _validator.Validate(Valid(maxDeliveryDistanceKm: (decimal)distance));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.MaxDeliveryDistanceKm));
    }

    [Fact]
    public void AddressTooLong_Fails()
    {
        var result = _validator.Validate(Valid(address: new string('x', 501)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.Address));
    }

    [Fact]
    public void PhoneTooLong_Fails()
    {
        var result = _validator.Validate(Valid(phone: new string('1', 51)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.Phone));
    }

    [Fact]
    public void BlankUpdatedBy_Fails()
    {
        var result = _validator.Validate(Valid(updatedBy: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateStoreParametersCommand.UpdatedBy));
    }
}
