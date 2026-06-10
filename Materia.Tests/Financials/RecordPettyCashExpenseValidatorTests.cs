using FluentAssertions;
using Materia.Application.Commands.Financials.RecordPettyCashExpense;
using Materia.Domain.Financials;

namespace Materia.Tests.Financials;

public class RecordPettyCashExpenseValidatorTests
{
    private readonly RecordPettyCashExpenseCommandValidator _validator = new();

    private static RecordPettyCashExpenseCommand Valid(
        decimal amount = 50_000m,
        string recipient = "Budi",
        PettyCashCategory category = PettyCashCategory.Bensin,
        string? reasonDetail = null,
        string? notes = null,
        Guid? idempotencyKey = null) =>
        new(amount, recipient, category, reasonDetail, notes, "admin",
            idempotencyKey ?? Guid.NewGuid());

    [Fact]
    public void Valid_PredefinedCategory_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Lainnya_WithDetail_Passes()
    {
        var result = _validator.Validate(
            Valid(category: PettyCashCategory.Lainnya, reasonDetail: "Konsumsi rapat"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void NonPositiveAmount_Fails(decimal amount)
    {
        var result = _validator.Validate(Valid(amount: amount));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.Amount));
    }

    [Fact]
    public void BlankRecipient_Fails()
    {
        var result = _validator.Validate(Valid(recipient: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.Recipient));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Lainnya_WithoutDetail_Fails(string? detail)
    {
        var result = _validator.Validate(
            Valid(category: PettyCashCategory.Lainnya, reasonDetail: detail));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.ReasonDetail));
    }

    [Fact]
    public void PredefinedCategory_WithoutDetail_Passes()
    {
        // "Sebutkan" is only required for Lainnya.
        var result = _validator.Validate(
            Valid(category: PettyCashCategory.SparepartKendaraan, reasonDetail: null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidCategoryEnum_Fails()
    {
        var result = _validator.Validate(Valid(category: (PettyCashCategory)99));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.Category));
    }

    [Fact]
    public void AmountAboveSanityCap_Fails()
    {
        var result = _validator.Validate(Valid(amount: 1_000_000_001m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.Amount));
    }

    [Fact]
    public void AmountAtSanityCap_Passes()
    {
        var result = _validator.Validate(Valid(amount: 1_000_000_000m));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(Valid(idempotencyKey: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordPettyCashExpenseCommand.IdempotencyKey));
    }
}
