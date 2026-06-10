using FluentAssertions;
using Materia.Application.Commands.Financials.RecordChangeFundDeposit;

namespace Materia.Tests.Financials;

public class RecordChangeFundDepositValidatorTests
{
    private readonly RecordChangeFundDepositCommandValidator _validator = new();

    private static RecordChangeFundDepositCommand Valid(
        decimal amount   = 100_000m,
        string? notes    = null,
        string recordedBy = "admin",
        Guid?  idempotencyKey = null) =>
        new(amount, notes, recordedBy, idempotencyKey ?? Guid.NewGuid());

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidCommand_WithNotes_Passes()
    {
        var result = _validator.Validate(Valid(notes: "Shift pagi"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50_000)]
    public void NonPositiveAmount_Fails(decimal amount)
    {
        var result = _validator.Validate(Valid(amount: amount));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundDepositCommand.Amount));
    }

    [Fact]
    public void AmountAboveSanityCap_Fails()
    {
        var result = _validator.Validate(Valid(amount: 1_000_000_001m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundDepositCommand.Amount));
    }

    [Fact]
    public void AmountAtSanityCap_Passes()
    {
        var result = _validator.Validate(Valid(amount: 1_000_000_000m));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankRecordedBy_Fails(string recordedBy)
    {
        var result = _validator.Validate(Valid(recordedBy: recordedBy));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundDepositCommand.RecordedBy));
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(Valid(idempotencyKey: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundDepositCommand.IdempotencyKey));
    }
}
