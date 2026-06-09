namespace Materia.Domain.Sales;

public enum SaleType      { Pickup, Delivery }

// NOTE: enum values are serialized by ordinal over the API (no JsonStringEnumConverter),
// and the WebUi mirrors this enum. Only ever APPEND new values — never reorder/insert.
public enum SaleStatus    { Draft, Confirmed, Paid, Cancelled, PartiallyPaid }
public enum PaymentMethod { Cash, BankTransfer, QRIS, Debit, Credit }
