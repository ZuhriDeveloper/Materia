using Materia.Domain.Sales;

namespace Materia.Application.Commands.Customers.RecordReceivablePayment;

public record RecordReceivablePaymentCommand(
    Guid          CustomerId,
    decimal       Amount,
    PaymentMethod Method,
    string?       Notes,
    string        ReceivedBy,
    Guid          IdempotencyKey);
