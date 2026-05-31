namespace Materia.Domain.Sales;

public enum SaleType      { Pickup, Delivery }
public enum SaleStatus    { Draft, Confirmed, Paid, Cancelled }
public enum PaymentMethod { Cash, BankTransfer, QRIS, Debit, Credit }
