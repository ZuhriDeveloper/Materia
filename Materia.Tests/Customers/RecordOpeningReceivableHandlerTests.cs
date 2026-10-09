using FluentAssertions;
using Materia.Application.Commands.Customers.RecordOpeningReceivable;
using Materia.Application.Contracts.Customers;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;

namespace Materia.Tests.Customers;

public class RecordOpeningReceivableHandlerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private Customer? _customer;

        public int SaveCount { get; private set; }
        public List<IDomainEvent> SavedEvents { get; } = [];

        public void Seed(Customer customer) => _customer = customer;

        public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
            => Task.FromResult(_customer?.Id == id ? _customer : null);

        public Task<StoredReceivablePayment?> FindReceivablePaymentByKeyAsync(
            Guid idempotencyKey, CancellationToken ct = default)
            => Task.FromResult<StoredReceivablePayment?>(null);

        public Task SaveAsync(Customer customer, CancellationToken ct = default)
        {
            SaveCount++;
            SavedEvents.AddRange(customer.DomainEvents);
            customer.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private static Customer ActiveCustomer()
    {
        var customer = Customer.Create("Budi Santoso", "08123456789", null, "admin");
        customer.ClearDomainEvents();
        return customer;
    }

    private static RecordOpeningReceivableCommand Command(
        Guid customerId, Guid receivableId, decimal amount = 150_000m) =>
        new(customerId, receivableId, amount,
            new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            "NOTA-LAMA-01", null, "admin");

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RecordsOpeningReceivableAndReturnsNewBalance()
    {
        var customer = ActiveCustomer();
        customer.IncurDebt(50_000m, Guid.NewGuid(), "INV-1", "kasir-01");
        customer.ClearDomainEvents();
        var repo = new FakeCustomerRepository();
        repo.Seed(customer);
        var handler      = new RecordOpeningReceivableCommandHandler(repo);
        var receivableId = Guid.NewGuid();

        var result = await handler.HandleAsync(Command(customer.Id.Value, receivableId));

        result.ReceivableId.Should().Be(receivableId);
        result.NewBalance.Should().Be(200_000m);
        repo.SaveCount.Should().Be(1);
        repo.SavedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OpeningReceivableRecorded>()
            .Which.RecordedBy.Should().Be("admin");
    }

    [Fact]
    public async Task Handle_UnknownCustomer_Throws()
    {
        var repo    = new FakeCustomerRepository();
        var handler = new RecordOpeningReceivableCommandHandler(repo);

        var act = () => handler.HandleAsync(Command(Guid.NewGuid(), Guid.NewGuid()));

        await act.Should().ThrowAsync<DomainException>();
        repo.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ResubmittedReceivableId_ThrowsWithoutSecondSave()
    {
        var customer = ActiveCustomer();
        var repo     = new FakeCustomerRepository();
        repo.Seed(customer);
        var handler      = new RecordOpeningReceivableCommandHandler(repo);
        var receivableId = Guid.NewGuid();
        await handler.HandleAsync(Command(customer.Id.Value, receivableId));

        var act = () => handler.HandleAsync(Command(customer.Id.Value, receivableId));

        await act.Should().ThrowAsync<DomainException>();
        repo.SaveCount.Should().Be(1);
        customer.OutstandingDebt.Should().Be(150_000m);
    }
}
