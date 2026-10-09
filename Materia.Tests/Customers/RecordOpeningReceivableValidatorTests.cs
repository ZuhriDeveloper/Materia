using FluentAssertions;
using Materia.Application.Commands.Customers.RecordOpeningReceivable;
using Materia.Domain.Customers;

namespace Materia.Tests.Customers;

public class RecordOpeningReceivableValidatorTests
{
    private readonly RecordOpeningReceivableCommandValidator _validator = new();

    private static RecordOpeningReceivableCommand Valid(
        Guid?     customerId   = null,
        Guid?     receivableId = null,
        decimal   amount       = 150_000m,
        DateTime? incurredAt   = null,
        string?   referenceNo  = "NOTA-LAMA-01",
        string?   notes        = null,
        string    recordedBy   = "admin") =>
        new(customerId ?? Guid.NewGuid(), receivableId ?? Guid.NewGuid(), amount,
            incurredAt ?? new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            referenceNo, notes, recordedBy);

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NullReferenceAndNotes_Passes()
    {
        _validator.Validate(Valid(referenceNo: null, notes: null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCustomerId_Fails()
    {
        var result = _validator.Validate(Valid(customerId: Guid.Empty));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.CustomerId));
    }

    [Fact]
    public void EmptyReceivableId_Fails()
    {
        var result = _validator.Validate(Valid(receivableId: Guid.Empty));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.ReceivableId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveAmount_Fails(decimal amount)
    {
        var result = _validator.Validate(Valid(amount: amount));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.Amount));
    }

    [Fact]
    public void AmountAboveCap_Fails()
    {
        var result = _validator.Validate(Valid(amount: Customer.MaxOpeningReceivableAmount + 1));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.Amount));
    }

    [Fact]
    public void FractionalRupiah_Fails()
    {
        var result = _validator.Validate(Valid(amount: 1_000.25m));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.Amount));
    }

    [Fact]
    public void IncurredAtBeforeMinimum_Fails()
    {
        var result = _validator.Validate(Valid(incurredAt: new DateTime(1900, 1, 1)));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.IncurredAt));
    }

    [Fact]
    public void DefaultIncurredAt_Fails()
    {
        var command = Valid() with { IncurredAt = default };
        var result = _validator.Validate(command);
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.IncurredAt));
    }

    [Fact]
    public void FutureIncurredAt_Fails()
    {
        var result = _validator.Validate(Valid(incurredAt: DateTime.UtcNow.AddDays(3)));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.IncurredAt));
    }

    [Fact]
    public void TooLongReference_Fails()
    {
        var result = _validator.Validate(Valid(referenceNo: new string('X', 51)));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.ReferenceNo));
    }

    [Fact]
    public void TooLongNotes_Fails()
    {
        var result = _validator.Validate(Valid(notes: new string('x', 501)));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.Notes));
    }

    [Fact]
    public void EmptyRecordedBy_Fails()
    {
        var result = _validator.Validate(Valid(recordedBy: ""));
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordOpeningReceivableCommand.RecordedBy));
    }
}
