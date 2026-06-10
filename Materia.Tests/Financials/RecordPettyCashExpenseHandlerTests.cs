using FluentAssertions;
using Materia.Application.Commands.Financials.RecordPettyCashExpense;
using Materia.Application.Contracts.Common;
using Materia.Application.Contracts.Financials;
using Materia.Domain.Financials;

namespace Materia.Tests.Financials;

/// <summary>
/// Tests the side-effect in <see cref="RecordPettyCashExpenseCommandHandler"/>:
/// when category == TukarUangKembalian the handler must also create and save a
/// <see cref="ChangeFundDeposit"/> with Source == PettyCashExchange — and a retried
/// submission must not duplicate the expense or that deposit.
/// </summary>
public class RecordPettyCashExpenseHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakePettyCashRepo : IPettyCashRepository
    {
        private readonly Dictionary<Guid, Guid> _byKey = new();

        public PettyCashExpense? Saved     { get; private set; }
        public int               SaveCount { get; private set; }

        public Task<PettyCashExpense?> GetByIdAsync(
            PettyCashExpenseId id, CancellationToken ct = default)
            => Task.FromResult<PettyCashExpense?>(null);

        public Task SaveAsync(PettyCashExpense expense, CancellationToken ct = default)
        {
            SaveCount++;
            Saved = expense;
            _byKey[expense.IdempotencyKey] = expense.Id.Value;
            expense.ClearDomainEvents();
            return Task.CompletedTask;
        }

        public Task<Guid?> FindExpenseIdByKeyAsync(Guid idempotencyKey, CancellationToken ct = default)
            => Task.FromResult<Guid?>(_byKey.TryGetValue(idempotencyKey, out var id) ? id : null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
            => action();
    }

    private sealed class FakeRefGen : IPettyCashReferenceGenerator
    {
        private int _counter;
        public Task<string> GenerateAsync(CancellationToken ct = default)
            => Task.FromResult($"KK-20260610-{++_counter:D4}");
    }

    private static RecordPettyCashExpenseCommandHandler Handler(
        FakePettyCashRepo pettyCashRepo, FakeChangeFundRepository changeFundRepo) =>
        new(pettyCashRepo, new FakeRefGen(), changeFundRepo, new FakeUnitOfWork());

    // ── TukarUangKembalian creates ChangeFundDeposit ─────────────────────────

    [Fact]
    public async Task TukarUangKembalian_AlsoSavesChangeFundDeposit_WithPettyCashExchangeSource()
    {
        var pettyCashRepo  = new FakePettyCashRepo();
        var changeFundRepo = new FakeChangeFundRepository();
        var key            = Guid.NewGuid();

        var command = new RecordPettyCashExpenseCommand(
            200_000m, "Kasir", PettyCashCategory.TukarUangKembalian,
            null, "Tukar uang kembalian shift pagi", "admin", key);

        await Handler(pettyCashRepo, changeFundRepo).HandleAsync(command);

        pettyCashRepo.Saved.Should().NotBeNull();

        var cf = changeFundRepo.LastDeposit;
        cf.Should().NotBeNull();
        cf!.Amount.Should().Be(200_000m);
        cf.Source.Should().Be(ChangeFundSource.PettyCashExchange);
        cf.SourceReferenceNo.Should().Be(pettyCashRepo.Saved!.ReferenceNo);
        cf.RecordedBy.Should().Be("admin");

        // The deposit shares the expense's client key — it is backed 1:1 by this
        // expense, so the same retry must dedupe both ledgers.
        cf.IdempotencyKey.Should().Be(key);
    }

    // ── Non-TukarUangKembalian does NOT create ChangeFundDeposit ─────────────

    [Fact]
    public async Task NonTukarUangKembalian_DoesNotSaveChangeFundDeposit()
    {
        var pettyCashRepo  = new FakePettyCashRepo();
        var changeFundRepo = new FakeChangeFundRepository();

        var command = new RecordPettyCashExpenseCommand(
            50_000m, "Budi", PettyCashCategory.Bensin,
            null, null, "admin", Guid.NewGuid());

        await Handler(pettyCashRepo, changeFundRepo).HandleAsync(command);

        pettyCashRepo.Saved.Should().NotBeNull();
        changeFundRepo.LastDeposit.Should().BeNull();
    }

    // ── Idempotency: sequential resend ────────────────────────────────────────

    [Fact]
    public async Task DuplicateKey_ReturnsPriorExpenseId_WithoutSecondExpenseOrDeposit()
    {
        var pettyCashRepo  = new FakePettyCashRepo();
        var changeFundRepo = new FakeChangeFundRepository();
        var handler        = Handler(pettyCashRepo, changeFundRepo);

        var command = new RecordPettyCashExpenseCommand(
            200_000m, "Kasir", PettyCashCategory.TukarUangKembalian,
            null, null, "admin", Guid.NewGuid());

        var first  = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);   // retried / double-clicked

        second.Should().Be(first);
        pettyCashRepo.SaveCount.Should().Be(1);
        changeFundRepo.DepositSaveCount.Should().Be(1);
    }
}
