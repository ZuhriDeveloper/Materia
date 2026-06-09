using FluentAssertions;
using Materia.Application.Commands.Customers.RecordReceivablePayment;
using Materia.Application.Contracts.Customers;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;
using Materia.Domain.Sales;

namespace Materia.Tests.Customers;

public class RecordReceivablePaymentHandlerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static Customer ActiveCustomerWithDebt(params (decimal amount, string refNo)[] invoices)
    {
        var customer = Customer.Create("Budi Santoso", "08123456789", null, "admin");
        foreach (var (amount, refNo) in invoices)
            customer.IncurDebt(amount, Guid.NewGuid(), refNo, "kasir-01");
        customer.ClearDomainEvents();
        return customer;
    }

    /// <summary>
    /// In-memory <see cref="ICustomerRepository"/> that mimics the real repository's
    /// idempotency contract: SaveAsync records a payment snapshot keyed by the event's
    /// IdempotencyKey, and FindReceivablePaymentByKeyAsync returns it on a resend.
    /// </summary>
    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, StoredReceivablePayment> _payments = new();
        private Customer? _customer;

        // Concurrent-race simulation: the "other" transaction commits during our SaveAsync.
        private Guid                     _raceKey;
        private StoredReceivablePayment? _racePayment;
        private bool                     _raceArmed;
        private bool                     _raceCommitted;

        public int SaveCount { get; private set; }

        public void Seed(Customer customer) => _customer = customer;

        public void ArmRace(Guid key, StoredReceivablePayment payment)
        {
            _raceKey     = key;
            _racePayment = payment;
            _raceArmed   = true;
        }

        public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
            => Task.FromResult(_customer);

        public Task<StoredReceivablePayment?> FindReceivablePaymentByKeyAsync(
            Guid idempotencyKey, CancellationToken ct = default)
        {
            if (_payments.TryGetValue(idempotencyKey, out var existing))
                return Task.FromResult<StoredReceivablePayment?>(existing);

            // The racing transaction only becomes visible once it has "committed".
            if (_raceCommitted && idempotencyKey == _raceKey)
                return Task.FromResult(_racePayment);

            return Task.FromResult<StoredReceivablePayment?>(null);
        }

        public Task SaveAsync(Customer customer, CancellationToken ct = default)
        {
            if (_raceArmed)
            {
                // Simulate the unique-index violation surfaced by a concurrent winner.
                _raceArmed     = false;
                _raceCommitted = true;
                throw new DuplicateReceivablePaymentException(_raceKey);
            }

            SaveCount++;
            foreach (var e in customer.DomainEvents.OfType<ReceivablePaymentRecorded>())
            {
                _payments[e.IdempotencyKey] = new StoredReceivablePayment(
                    e.PaymentId,
                    e.NewBalance,
                    e.Allocations
                        .Select(a => new StoredReceivableAllocation(
                            a.SaleId, a.ReferenceNo, a.AppliedAmount, a.RemainingAfter))
                        .ToList());
            }

            customer.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private static RecordReceivablePaymentCommand Command(
        Customer customer, Guid key, decimal amount = 60_000m) =>
        new(customer.Id.Value, amount, PaymentMethod.Cash, null, "kasir-01", key);

    // ── First payment: normal path ─────────────────────────────────────────────

    [Fact]
    public async Task FirstPayment_RecordsAndReturnsResult()
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));
        var repo     = new FakeCustomerRepository();
        repo.Seed(customer);
        var handler  = new RecordReceivablePaymentCommandHandler(repo);

        var result = await handler.HandleAsync(Command(customer, Guid.NewGuid()));

        result.NewBalance.Should().Be(40_000m);
        result.PaymentId.Should().NotBeEmpty();
        result.Allocations.Should().ContainSingle();
        repo.SaveCount.Should().Be(1);
        customer.OutstandingDebt.Should().Be(40_000m);
    }

    // ── Sequential resend (the M1 scenario) ─────────────────────────────────────

    [Fact]
    public async Task DuplicateKey_ReturnsPriorResult_WithoutSecondSave()
    {
        var customer = ActiveCustomerWithDebt((100_000m, "INV-1"));
        var repo     = new FakeCustomerRepository();
        repo.Seed(customer);
        var handler  = new RecordReceivablePaymentCommandHandler(repo);
        var key      = Guid.NewGuid();
        var command  = Command(customer, key);

        var first  = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);   // retried / double-clicked

        // Only the first request was persisted; the balance was reduced exactly once.
        repo.SaveCount.Should().Be(1);
        customer.OutstandingDebt.Should().Be(40_000m);

        // The resend returns the prior result rather than recording a new payment.
        second.PaymentId.Should().Be(first.PaymentId);
        second.NewBalance.Should().Be(first.NewBalance);
    }

    // ── Concurrent race: unique index rejects the second insert ─────────────────

    [Fact]
    public async Task ConcurrentDuplicate_RepositoryRejects_HandlerReturnsPriorResult()
    {
        var customer        = ActiveCustomerWithDebt((100_000m, "INV-1"));
        var repo            = new FakeCustomerRepository();
        repo.Seed(customer);
        var handler         = new RecordReceivablePaymentCommandHandler(repo);
        var key             = Guid.NewGuid();
        var priorPaymentId  = Guid.NewGuid();

        // The other in-flight request will commit first and own the key.
        repo.ArmRace(key, new StoredReceivablePayment(
            priorPaymentId, 40_000m,
            [new StoredReceivableAllocation(Guid.NewGuid(), "INV-1", 60_000m, 0m)]));

        var result = await handler.HandleAsync(Command(customer, key));

        result.PaymentId.Should().Be(priorPaymentId);
        result.NewBalance.Should().Be(40_000m);
        result.Allocations.Should().ContainSingle();
    }
}
