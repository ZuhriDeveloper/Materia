using FluentAssertions;
using Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;

namespace Materia.Tests.Financials;

public class RecordChangeFundWithdrawalValidatorTests
{
    private readonly RecordChangeFundWithdrawalCommandValidator _validator = new();

    private static RecordChangeFundWithdrawalCommand Valid(
        decimal amount     = 50_000m,
        string  reason     = "Koreksi: deposit salah catat",
        string  recordedBy = "admin",
        Guid?   idempotencyKey = null) =>
        new(amount, reason, recordedBy, idempotencyKey ?? Guid.NewGuid());

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
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
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.Amount));
    }

    [Fact]
    public void AmountAboveSanityCap_Fails()
    {
        var result = _validator.Validate(Valid(amount: 1_000_000_001m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankReason_Fails(string reason)
    {
        var result = _validator.Validate(Valid(reason: reason));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.Reason));
    }

    [Fact]
    public void ReasonTooLong_Fails()
    {
        var result = _validator.Validate(Valid(reason: new string('x', 301)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.Reason));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankRecordedBy_Fails(string recordedBy)
    {
        var result = _validator.Validate(Valid(recordedBy: recordedBy));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.RecordedBy));
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(Valid(idempotencyKey: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordChangeFundWithdrawalCommand.IdempotencyKey));
    }
}
