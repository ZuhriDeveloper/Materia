using FluentAssertions;
using Materia.Application.Commands.Sales.RecordSaleReturn;

namespace Materia.Tests.Sales;

public class RecordSaleReturnValidatorTests
{
    private readonly RecordSaleReturnCommandValidator _validator = new();

    private static ReturnLineDto ValidLine() =>
        new(Guid.NewGuid(), null, "sak", 2m);

    private static RecordSaleReturnCommand Valid(
        Guid?                         originalSaleId = null,
        IReadOnlyList<ReturnLineDto>? lines          = null,
        string                        resolution     = "CashRefund",
        string                        reason         = "Barang cacat",
        string                        returnedBy     = "kasir-01") =>
        new(
            originalSaleId ?? Guid.NewGuid(),
            lines ?? [ValidLine()],
            resolution,
            reason,
            returnedBy);

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyOriginalSaleId_Fails()
    {
        var result = _validator.Validate(Valid(originalSaleId: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordSaleReturnCommand.OriginalSaleId));
    }

    [Fact]
    public void EmptyLines_Fails()
    {
        var result = _validator.Validate(Valid(lines: []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordSaleReturnCommand.Lines));
    }

    [Fact]
    public void ZeroQuantityLine_Fails()
    {
        var badLine = new ReturnLineDto(Guid.NewGuid(), null, "sak", 0m);
        var result = _validator.Validate(Valid(lines: [badLine]));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains(nameof(ReturnLineDto.Quantity)));
    }

    [Fact]
    public void NegativeQuantityLine_Fails()
    {
        var badLine = new ReturnLineDto(Guid.NewGuid(), null, "sak", -1m);
        var result = _validator.Validate(Valid(lines: [badLine]));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains(nameof(ReturnLineDto.Quantity)));
    }

    [Fact]
    public void InvalidResolution_Fails()
    {
        var result = _validator.Validate(Valid(resolution: "GiveCandy"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordSaleReturnCommand.Resolution));
    }

    [Fact]
    public void EmptyReason_Fails()
    {
        var result = _validator.Validate(Valid(reason: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RecordSaleReturnCommand.Reason));
    }

    [Fact]
    public void DebtReductionResolution_Passes()
    {
        var result = _validator.Validate(Valid(resolution: "DebtReduction"));
        result.IsValid.Should().BeTrue();
    }
}
