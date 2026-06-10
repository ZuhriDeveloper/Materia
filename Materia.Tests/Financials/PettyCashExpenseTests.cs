using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Financials;
using Materia.Domain.Financials.Events;

namespace Materia.Tests.Financials;

public class PettyCashExpenseTests
{
    private const string Ref  = "KK-20260609-0001";
    private const string User = "admin";

    [Fact]
    public void Record_WithValidData_RaisesPettyCashExpenseRecorded()
    {
        var expense = PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Bensin, null, "Isi tangki", Ref, User);

        var evt = expense.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PettyCashExpenseRecorded>().Subject;

        evt.Amount.Should().Be(50_000m);
        evt.Recipient.Should().Be("Budi");
        evt.Category.Should().Be(PettyCashCategory.Bensin);
        evt.ReasonDetail.Should().BeNull();
        evt.Notes.Should().Be("Isi tangki");
        evt.ReferenceNo.Should().Be(Ref);
        evt.RecordedBy.Should().Be(User);

        expense.Amount.Should().Be(50_000m);
        expense.Reason.Should().Be("Bensin");
        expense.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50_000)]
    public void Record_WithNonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        Action act = () => PettyCashExpense.Record(
            amount, "Budi", PettyCashCategory.Bensin, null, null, Ref, User);

        act.Should().Throw<DomainException>().WithMessage("*lebih dari nol*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_WithBlankRecipient_ThrowsDomainException(string recipient)
    {
        Action act = () => PettyCashExpense.Record(
            50_000m, recipient, PettyCashCategory.Bensin, null, null, Ref, User);

        act.Should().Throw<DomainException>().WithMessage("*Penerima*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_LainnyaWithoutDetail_ThrowsDomainException(string? detail)
    {
        Action act = () => PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Lainnya, detail, null, Ref, User);

        act.Should().Throw<DomainException>().WithMessage("*Sebutkan*");
    }

    [Fact]
    public void Record_LainnyaWithDetail_KeepsDetailAndUsesItAsReason()
    {
        var expense = PettyCashExpense.Record(
            75_000m, "Andi", PettyCashCategory.Lainnya, "  Konsumsi rapat  ", null, Ref, User);

        expense.ReasonDetail.Should().Be("Konsumsi rapat");
        expense.Reason.Should().Be("Konsumsi rapat");
    }

    [Fact]
    public void Record_NonLainnyaWithStrayDetail_NormalizesDetailToNull()
    {
        var expense = PettyCashExpense.Record(
            30_000m, "Andi", PettyCashCategory.SparepartKendaraan, "ignored", null, Ref, User);

        expense.ReasonDetail.Should().BeNull();
        expense.Reason.Should().Be("Sparepart Kendaraan");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_WithBlankReference_ThrowsDomainException(string referenceNo)
    {
        Action act = () => PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Bensin, null, null, referenceNo, User);

        act.Should().Throw<DomainException>().WithMessage("*referensi*");
    }

    [Fact]
    public void Record_TrimsRecipientNotesAndReference()
    {
        var expense = PettyCashExpense.Record(
            50_000m, "  Budi  ", PettyCashCategory.Bensin, null, "  catatan  ", "  KK-1  ", User);

        expense.Recipient.Should().Be("Budi");
        expense.Notes.Should().Be("catatan");
        expense.ReferenceNo.Should().Be("KK-1");
    }

    [Fact]
    public void Record_WithBlankNotes_StoresNull()
    {
        var expense = PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Bensin, null, "   ", Ref, User);

        expense.Notes.Should().BeNull();
    }

    // ── Idempotency key ───────────────────────────────────────────────────────

    [Fact]
    public void Record_WithIdempotencyKey_BakesKeyIntoEvent()
    {
        var key = Guid.NewGuid();

        var expense = PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Bensin, null, null, Ref, User, key);

        expense.DomainEvents.OfType<PettyCashExpenseRecorded>().Single()
            .IdempotencyKey.Should().Be(key);
        expense.IdempotencyKey.Should().Be(key);
    }

    [Fact]
    public void Record_WithoutIdempotencyKey_GeneratesNonEmptyKey()
    {
        var expense = PettyCashExpense.Record(
            50_000m, "Budi", PettyCashCategory.Bensin, null, null, Ref, User);

        expense.IdempotencyKey.Should().NotBeEmpty();
    }

    [Fact]
    public void Reconstitute_FromEvents_RestoresStateAndClearsPendingEvents()
    {
        var original = PettyCashExpense.Record(
            120_000m, "Citra", PettyCashCategory.Lainnya, "Servis AC", "urgent", Ref, User);

        var reconstituted = PettyCashExpense.Reconstitute(original.DomainEvents);

        reconstituted.Id.Should().Be(original.Id);
        reconstituted.Amount.Should().Be(120_000m);
        reconstituted.Recipient.Should().Be("Citra");
        reconstituted.Category.Should().Be(PettyCashCategory.Lainnya);
        reconstituted.ReasonDetail.Should().Be("Servis AC");
        reconstituted.Reason.Should().Be("Servis AC");
        reconstituted.Notes.Should().Be("urgent");
        reconstituted.ReferenceNo.Should().Be(Ref);
        reconstituted.IdempotencyKey.Should().Be(original.IdempotencyKey);
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
