namespace Materia.Application.Commands.Customers.RecordOpeningReceivable;

public record RecordOpeningReceivableResult(
    Guid    ReceivableId,
    decimal NewBalance);
