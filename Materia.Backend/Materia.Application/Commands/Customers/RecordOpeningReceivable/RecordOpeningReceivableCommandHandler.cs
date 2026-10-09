using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;

namespace Materia.Application.Commands.Customers.RecordOpeningReceivable;

public class RecordOpeningReceivableCommandHandler(ICustomerRepository repository)
{
    public async Task<RecordOpeningReceivableResult> HandleAsync(
        RecordOpeningReceivableCommand command, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(
            new CustomerId(command.CustomerId), ct)
            ?? throw new DomainException("Pelanggan tidak ditemukan.");

        customer.RecordOpeningReceivable(
            command.ReceivableId, command.Amount, command.IncurredAt,
            command.ReferenceNo, command.Notes, command.RecordedBy);

        await repository.SaveAsync(customer, ct);

        return new RecordOpeningReceivableResult(command.ReceivableId, customer.OutstandingDebt);
    }
}
