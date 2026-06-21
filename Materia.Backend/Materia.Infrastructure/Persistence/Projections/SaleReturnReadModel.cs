using Materia.Domain.Sales;

namespace Materia.Infrastructure.Persistence.Projections;

public class SaleReturnReadModel
{
    public Guid             Id                  { get; set; }
    public Guid             StoreId             { get; set; }
    public Guid             OriginalSaleId      { get; set; }
    public string           OriginalReferenceNo { get; set; } = default!;
    public decimal          TotalRefundAmount   { get; set; }
    public ReturnResolution Resolution          { get; set; }
    public string           Reason              { get; set; } = default!;
    public string           ReturnedBy          { get; set; } = default!;
    public DateTime         ReturnedAt          { get; set; }
    public List<SaleReturnLineReadModel> Lines  { get; set; } = [];
}
