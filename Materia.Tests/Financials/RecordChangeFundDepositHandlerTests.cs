using FluentAssertions;
using Materia.Application.Commands.Financials.RecordChangeFundDeposit;
using Materia.Domain.Financials;

namespace Materia.Tests.Financials;

/// <summary>
/// Idempotency tests for <see cref="RecordChangeFundDepositCommandHandler"/>: a retried
/// or double-submitted POST must never append a second ChangeFundDeposited event
/// (the ledger is append-only, so a duplicate would permanently inflate the balance).
/// </summary>
public class RecordChangeFundDepositHandlerTests
{
    private static RecordChangeFundDepositCommand Command(Guid key, decimal amount = 100_000m) =>
        new(amount, null, "admin", key);

    // ── First deposit: normal path ──────────────────────────────────────────────

    [Fact]
    public async Task FirstDeposit_SavesAndReturnsId()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundDepositCommandHandler(repo);

        var id = await handler.HandleAsync(Command(Guid.NewGuid()));

        id.Should().NotBeEmpty();
        repo.DepositSaveCount.Should().Be(1);
        repo.LastDeposit!.Source.Should().Be(ChangeFundSource.ManualEntry);
    }

    // ── Sequential resend ───────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateKey_ReturnsPriorId_WithoutSecondSave()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundDepositCommandHandler(repo);
        var command = Command(Guid.NewGuid());

        var first  = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);   // retried / double-clicked

        repo.DepositSaveCount.Should().Be(1);
        second.Should().Be(first);
    }

    // ── Concurrent race: unique index rejects the second insert ────────────────

    [Fact]
    public async Task ConcurrentDuplicate_RepositoryRejects_HandlerReturnsPriorId()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundDepositCommandHandler(repo);
        var key     = Guid.NewGuid();
        var priorId = Guid.NewGuid();

        // The other in-flight request will commit first and own the key.
        repo.ArmRace(key, priorId);

        var id = await handler.HandleAsync(Command(key));

        id.Should().Be(priorId);
        repo.DepositSaveCount.Should().Be(0);
    }
}
