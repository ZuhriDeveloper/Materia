using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Financials;
using Materia.Domain.Financials.Events;

namespace Materia.Tests.Financials;

public class ChangeFundWithdrawalTests
{
    private const string User   = "admin";
    private const string Reason = "Koreksi: deposit dicatat dua kali";

    // ── Record — happy path ───────────────────────────────────────────────────

    [Fact]
    public void Record_RaisesChangeFundWithdrawn()
    {
        var withdrawal = ChangeFundWithdrawal.Record(50_000m, Reason, User);

        var evt = withdrawal.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ChangeFundWithdrawn>().Subject;

        evt.Amount.Should().Be(50_000m);
        evt.Reason.Should().Be(Reason);
        evt.RecordedBy.Should().Be(User);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        withdrawal.Amount.Should().Be(50_000m);
        withdrawal.Reason.Should().Be(Reason);
        withdrawal.RecordedBy.Should().Be(User);
        withdrawal.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── Domain rules ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50_000)]
    public void Record_NonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        Action act = () => ChangeFundWithdrawal.Record(amount, Reason, User);

        act.Should().Throw<DomainException>().WithMessage("*lebih dari nol*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_BlankReason_ThrowsDomainException(string? reason)
    {
        Action act = () => ChangeFundWithdrawal.Record(50_000m, reason!, User);

        act.Should().Throw<DomainException>().WithMessage("*Alasan*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_BlankRecordedBy_ThrowsDomainException(string? recordedBy)
    {
        Action act = () => ChangeFundWithdrawal.Record(50_000m, Reason, recordedBy!);

        act.Should().Throw<DomainException>().WithMessage("*Pencatat*");
    }

    // ── Amount rounding ───────────────────────────────────────────────────────

    [Fact]
    public void Record_AmountIsRoundedToTwoDecimals()
    {
        var withdrawal = ChangeFundWithdrawal.Record(49_999.999m, Reason, User);

        withdrawal.Amount.Should().Be(Math.Round(49_999.999m, 2, MidpointRounding.AwayFromZero));
    }

    // ── Idempotency key ───────────────────────────────────────────────────────

    [Fact]
    public void Record_WithIdempotencyKey_BakesKeyIntoEvent()
    {
        var key = Guid.NewGuid();

        var withdrawal = ChangeFundWithdrawal.Record(50_000m, Reason, User, key);

        withdrawal.DomainEvents.OfType<ChangeFundWithdrawn>().Single()
            .IdempotencyKey.Should().Be(key);
        withdrawal.IdempotencyKey.Should().Be(key);
    }

    [Fact]
    public void Record_WithoutIdempotencyKey_GeneratesNonEmptyKey()
    {
        var withdrawal = ChangeFundWithdrawal.Record(50_000m, Reason, User);

        withdrawal.IdempotencyKey.Should().NotBeEmpty();
    }

    // ── Reconstitute ─────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromEvents_RestoresStateAndClearsPendingEvents()
    {
        var original = ChangeFundWithdrawal.Record(75_000m, Reason, User);

        var reconstituted = ChangeFundWithdrawal.Reconstitute(original.DomainEvents);

        reconstituted.Id.Should().Be(original.Id);
        reconstituted.Amount.Should().Be(75_000m);
        reconstituted.Reason.Should().Be(Reason);
        reconstituted.RecordedBy.Should().Be(User);
        reconstituted.IdempotencyKey.Should().Be(original.IdempotencyKey);
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
