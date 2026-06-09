using Materia.Domain.Sales;

namespace Materia.Infrastructure.Persistence.Projections;

public class SaleReadModel
{
    public Guid          Id                { get; set; }
    public string        ReferenceNo       { get; set; } = default!;
    public Guid?         CustomerId        { get; set; }
    public string        CustomerName      { get; set; } = "Umum";
    public Guid?         CustomerAddressId { get; set; }
    public string?       DeliveryAddress   { get; set; }
    public SaleType      SaleType          { get; set; }
    public SaleStatus    Status            { get; set; }
    public bool          IsDeliveryRequired { get; set; }
    public decimal       Subtotal          { get; set; }
    public decimal       Discount          { get; set; }
    public decimal       Tax               { get; set; }
    public decimal       GrandTotal        { get; set; }
    public decimal       AmountPaid        { get; set; }
    public decimal       OutstandingAmount { get; set; }
    public string        CreatedBy         { get; set; } = default!;
    public string?       ServedBy          { get; set; }
    public DateTime      CreatedAt         { get; set; }

    // Payment (nullable, denormalized)
    public decimal?       PaidAmount    { get; set; }
    public decimal?       Change        { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime?      PaidAt        { get; set; }

    public List<SaleItemReadModel> Items { get; set; } = [];
}
