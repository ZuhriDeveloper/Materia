using FluentAssertions;
using Materia.Application.Commands.Financials.RecordChangeFundWithdrawal;
using Materia.Application.Contracts.Financials;
using Materia.Application.DTOs.Inventory;
using Materia.Domain.Common;

namespace Materia.Tests.Financials;

/// <summary>
/// Tests for <see cref="RecordChangeFundWithdrawalCommandHandler"/> — the Admin-only
/// compensating entry that corrects an erroneous deposit. A withdrawal must never
/// exceed the current balance (the fund cannot go negative) and must be idempotent.
/// </summary>
public class RecordChangeFundWithdrawalHandlerTests
{
    private sealed class FakeQueryRepo(decimal balance) : IChangeFundQueryRepository
    {
        public Task<PagedResult<ChangeFundDepositDto>> GetPagedAsync(
            int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<ChangeFundDepositDto>([], 0, page, pageSize));

        public Task<PagedResult<ChangeFundWithdrawalDto>> GetPagedWithdrawalsAsync(
            int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<ChangeFundWithdrawalDto>([], 0, page, pageSize));

        public Task<decimal> GetTotalBalanceAsync(CancellationToken ct = default)
            => Task.FromResult(balance);
    }

    private static RecordChangeFundWithdrawalCommand Command(
        Guid key, decimal amount = 50_000m) =>
        new(amount, "Koreksi: deposit salah catat", "admin", key);

    // ── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Withdrawal_WithinBalance_SavesAndReturnsId()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundWithdrawalCommandHandler(
            repo, new FakeQueryRepo(balance: 200_000m));

        var id = await handler.HandleAsync(Command(Guid.NewGuid()));

        id.Should().NotBeEmpty();
        repo.WithdrawalSaveCount.Should().Be(1);
        repo.LastWithdrawal!.Amount.Should().Be(50_000m);
    }

    // ── Balance guard ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Withdrawal_ExceedingBalance_ThrowsDomainException()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundWithdrawalCommandHandler(
            repo, new FakeQueryRepo(balance: 30_000m));

        var act = () => handler.HandleAsync(Command(Guid.NewGuid(), amount: 50_000m));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*saldo*");
        repo.WithdrawalSaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Withdrawal_EqualToBalance_IsAllowed()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundWithdrawalCommandHandler(
            repo, new FakeQueryRepo(balance: 50_000m));

        var id = await handler.HandleAsync(Command(Guid.NewGuid(), amount: 50_000m));

        id.Should().NotBeEmpty();
        repo.WithdrawalSaveCount.Should().Be(1);
    }

    // ── Sequential resend ───────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateKey_ReturnsPriorId_WithoutSecondSave()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundWithdrawalCommandHandler(
            repo, new FakeQueryRepo(balance: 200_000m));
        var command = Command(Guid.NewGuid());

        var first  = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);   // retried / double-clicked

        repo.WithdrawalSaveCount.Should().Be(1);
        second.Should().Be(first);
    }

    // ── Concurrent race: unique index rejects the second insert ────────────────

    [Fact]
    public async Task ConcurrentDuplicate_RepositoryRejects_HandlerReturnsPriorId()
    {
        var repo    = new FakeChangeFundRepository();
        var handler = new RecordChangeFundWithdrawalCommandHandler(
            repo, new FakeQueryRepo(balance: 200_000m));
        var key     = Guid.NewGuid();
        var priorId = Guid.NewGuid();

        repo.ArmRace(key, priorId);

        var id = await handler.HandleAsync(Command(key));

        id.Should().Be(priorId);
        repo.WithdrawalSaveCount.Should().Be(0);
    }
}
