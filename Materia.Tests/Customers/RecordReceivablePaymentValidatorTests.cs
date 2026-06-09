using FluentAssertions;
using Materia.Application.Commands.Customers.RecordReceivablePayment;
using Materia.Domain.Sales;

namespace Materia.Tests.Customers;

public class RecordReceivablePaymentValidatorTests
{
    private readonly RecordReceivablePaymentCommandValidator _validator = new();

    private static RecordReceivablePaymentCommand Valid(
        Guid?         customerId     = null,
        decimal       amount         = 50_000m,
        PaymentMethod method         = PaymentMethod.Cash,
        string?       notes          = null,
        string        receivedBy     = "kasir-01",
        Guid?         idempotencyKey = null) =>
        new(customerId ?? Guid.NewGuid(), amount, method, notes, receivedBy,
            idempotencyKey ?? Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
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
            e.PropertyName == nameof(RecordReceivablePaymentCommand.Amount));
    }

    [Fact]
    public void EmptyCustomerId_Fails()
    {
        var result = _validator.Validate(Valid(customerId: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordReceivablePaymentCommand.CustomerId));
    }

    [Fact]
    public void InvalidPaymentMethodEnum_Fails()
    {
        var result = _validator.Validate(Valid(method: (PaymentMethod)99));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordReceivablePaymentCommand.Method));
    }

    [Fact]
    public void Notes_TooLong_Fails()
    {
        var longNotes = new string('x', 501);
        var result = _validator.Validate(Valid(notes: longNotes));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordReceivablePaymentCommand.Notes));
    }

    [Fact]
    public void Notes_ExactlyMaxLength_Passes()
    {
        var maxNotes = new string('x', 500);
        var result = _validator.Validate(Valid(notes: maxNotes));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankReceivedBy_Fails(string receivedBy)
    {
        var result = _validator.Validate(Valid(receivedBy: receivedBy));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordReceivablePaymentCommand.ReceivedBy));
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        // A missing/empty key would defeat double-submit protection, so it is rejected.
        var result = _validator.Validate(Valid(idempotencyKey: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordReceivablePaymentCommand.IdempotencyKey));
    }
}
