using Materia.Domain.Common;
using Materia.Domain.Sales.Events;

namespace Materia.Domain.Sales;

public sealed class SaleReturn : AggregateRoot<SaleReturnId>
{
    private readonly List<SaleReturnLine> _lines = [];

    public SaleId           OriginalSaleId      { get; private set; }
    public string           OriginalReferenceNo { get; private set; } = default!;
    public decimal          TotalRefundAmount   { get; private set; }
    public ReturnResolution Resolution          { get; private set; }
    public string           Reason              { get; private set; } = default!;
    public string           ReturnedBy          { get; private set; } = default!;
    public DateTime         ReturnedAt          { get; private set; }
    public IReadOnlyList<SaleReturnLine> Lines  => _lines.AsReadOnly();

    private SaleReturn() { }

    public static SaleReturn Record(
        SaleId originalSaleId, string originalReferenceNo,
        IReadOnlyList<SaleReturnLineInput> lines,
        ReturnResolution resolution, string reason, string returnedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Alasan retur tidak boleh kosong.");
        if (string.IsNullOrWhiteSpace(returnedBy))
            throw new DomainException("Staff yang mencatat retur tidak boleh kosong.");
        if (lines.Count == 0)
            throw new DomainException("Retur harus memiliki minimal satu item.");
        if (lines.Any(l => l.Quantity <= 0))
            throw new DomainException("Kuantitas retur harus lebih dari nol.");

        var lineData = lines.Select(l => new SaleReturnLineData(
            l.ProductId, l.ProductName, l.VariantId, l.ColorName,
            l.UnitName, l.Quantity, l.QuantityInBaseUnit, l.UnitPrice)).ToList();

        var totalRefund = lineData.Sum(l => l.Quantity * l.UnitPrice);

        var ret = new SaleReturn();
        ret.Raise(new SaleReturnRecorded(
            SaleReturnId.New(), originalSaleId, originalReferenceNo.Trim(),
            lineData.AsReadOnly(), totalRefund, resolution,
            reason.Trim(), returnedBy.Trim(), DateTime.UtcNow));
        return ret;
    }

    public static SaleReturn Reconstitute(IEnumerable<IDomainEvent> events)
    {
        var ret = new SaleReturn();
        ret.Load(events);
        return ret;
    }

    protected override void Apply(IDomainEvent domainEvent)
    {
        if (domainEvent is not SaleReturnRecorded e) return;

        Id                  = e.ReturnId;
        OriginalSaleId      = e.OriginalSaleId;
        OriginalReferenceNo = e.OriginalReferenceNo;
        TotalRefundAmount   = e.TotalRefundAmount;
        Resolution          = e.Resolution;
        Reason              = e.Reason;
        ReturnedBy          = e.ReturnedBy;
        ReturnedAt          = e.OccurredAt;
        _lines.AddRange(e.Lines.Select(l =>
            new SaleReturnLine(l.ProductId, l.ProductName, l.VariantId, l.ColorName,
                               l.UnitName, l.Quantity, l.QuantityInBaseUnit, l.UnitPrice)));
    }
}
