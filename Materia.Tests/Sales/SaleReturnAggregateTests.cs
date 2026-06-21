using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Sales;
using Materia.Domain.Sales.Events;

namespace Materia.Tests.Sales;

public class SaleReturnAggregateTests
{
    private static readonly SaleId OriginalSaleId = SaleId.From(Guid.NewGuid());
    private const string RefNo      = "INV-0001";
    private const string Reason     = "Barang cacat";
    private const string ReturnedBy = "kasir-01";

    private static IReadOnlyList<SaleReturnLineInput> OneValidLine() =>
    [
        new SaleReturnLineInput(
            ProductId:          Guid.NewGuid(),
            ProductName:        "Semen 50kg",
            VariantId:          null,
            ColorName:          null,
            UnitName:           "sak",
            Quantity:           2m,
            QuantityInBaseUnit: 2m,
            UnitPrice:          65_000m)
    ];

    [Fact]
    public void Record_ValidInput_RaisesSaleReturnRecordedEvent()
    {
        var saleReturn = SaleReturn.Record(
            OriginalSaleId, RefNo, OneValidLine(),
            ReturnResolution.CashRefund, Reason, ReturnedBy);

        saleReturn.DomainEvents.Should().HaveCount(1);
        var evt = saleReturn.DomainEvents.OfType<SaleReturnRecorded>().Single();
        evt.ReturnId.Value.Should().NotBeEmpty();
        evt.OriginalSaleId.Should().Be(OriginalSaleId);
        evt.OriginalReferenceNo.Should().Be(RefNo);
        evt.Resolution.Should().Be(ReturnResolution.CashRefund);
        evt.Reason.Should().Be(Reason);
        evt.ReturnedBy.Should().Be(ReturnedBy);
        evt.Lines.Should().HaveCount(1);

        saleReturn.Id.Value.Should().NotBeEmpty();
        saleReturn.OriginalSaleId.Should().Be(OriginalSaleId);
    }

    [Fact]
    public void Reconstitute_FromEvents_RebuildsState()
    {
        var original = SaleReturn.Record(
            OriginalSaleId, RefNo, OneValidLine(),
            ReturnResolution.DebtReduction, Reason, ReturnedBy);

        var events = original.DomainEvents.ToList();
        original.ClearDomainEvents();

        var restored = SaleReturn.Reconstitute(events);

        restored.DomainEvents.Should().BeEmpty();
        restored.Id.Should().Be(original.Id);
        restored.OriginalSaleId.Should().Be(OriginalSaleId);
        restored.OriginalReferenceNo.Should().Be(RefNo);
        restored.Resolution.Should().Be(ReturnResolution.DebtReduction);
        restored.Reason.Should().Be(Reason);
        restored.ReturnedBy.Should().Be(ReturnedBy);
        restored.Lines.Should().HaveCount(1);
        restored.Lines[0].ProductName.Should().Be("Semen 50kg");
        restored.Lines[0].Quantity.Should().Be(2m);
        restored.Lines[0].UnitPrice.Should().Be(65_000m);
    }

    [Fact]
    public void Record_EmptyReason_ThrowsDomainException()
    {
        var act = () => SaleReturn.Record(
            OriginalSaleId, RefNo, OneValidLine(),
            ReturnResolution.CashRefund, reason: "", ReturnedBy);

        act.Should().Throw<DomainException>()
            .WithMessage("*Alasan retur*");
    }

    [Fact]
    public void Record_EmptyLines_ThrowsDomainException()
    {
        var act = () => SaleReturn.Record(
            OriginalSaleId, RefNo, [],
            ReturnResolution.CashRefund, Reason, ReturnedBy);

        act.Should().Throw<DomainException>()
            .WithMessage("*minimal satu item*");
    }

    [Fact]
    public void Record_ZeroQuantityLine_ThrowsDomainException()
    {
        var badLine = new SaleReturnLineInput(
            Guid.NewGuid(), "Semen", null, null, "sak", 0m, 0m, 65_000m);

        var act = () => SaleReturn.Record(
            OriginalSaleId, RefNo, [badLine],
            ReturnResolution.CashRefund, Reason, ReturnedBy);

        act.Should().Throw<DomainException>()
            .WithMessage("*Kuantitas retur*");
    }

    [Fact]
    public void Record_TotalRefundAmount_IsSumOfQtyTimesPrice()
    {
        var lines = new List<SaleReturnLineInput>
        {
            new(Guid.NewGuid(), "Produk A", null, null, "pcs", 3m, 3m, 10_000m),
            new(Guid.NewGuid(), "Produk B", null, null, "pcs", 2m, 2m, 25_000m),
        };

        var saleReturn = SaleReturn.Record(
            OriginalSaleId, RefNo, lines,
            ReturnResolution.CashRefund, Reason, ReturnedBy);

        // 3*10_000 + 2*25_000 = 80_000
        saleReturn.TotalRefundAmount.Should().Be(80_000m);
    }
}
