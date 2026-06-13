using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class PaymentTermTests
{
    [Fact]
    public void DueDateFrom_Days_AddsDays()
    {
        var anchor = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        new PaymentTerm(10, PaymentTermUnit.Days).DueDateFrom(anchor)
            .Should().Be(new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DueDateFrom_Weeks_AddsSevenDaysPerWeek()
    {
        var anchor = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        new PaymentTerm(2, PaymentTermUnit.Weeks).DueDateFrom(anchor)
            .Should().Be(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DueDateFrom_Months_AddsCalendarMonths()
    {
        // Calendar months, not 30/60 days — Jan 31 + 1 month clamps to Feb.
        var anchor = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        new PaymentTerm(1, PaymentTermUnit.Months).DueDateFrom(anchor)
            .Should().Be(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Ctor_NonPositiveValue_Throws(int value)
    {
        Action act = () => _ = new PaymentTerm(value, PaymentTermUnit.Months);
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void FromRaw_NullValueOrUnit_ReturnsNull()
    {
        PaymentTerm.FromRaw(null, "Months").Should().BeNull();
        PaymentTerm.FromRaw(2, null).Should().BeNull();
        PaymentTerm.FromRaw(2, "").Should().BeNull();
    }

    [Fact]
    public void FromRaw_ValidValues_RoundTrips()
    {
        var term = PaymentTerm.FromRaw(6, "months");
        term.Should().Be(new PaymentTerm(6, PaymentTermUnit.Months));
    }

    [Fact]
    public void FromRaw_UnknownUnit_Throws()
    {
        Action act = () => PaymentTerm.FromRaw(1, "fortnights");
        act.Should().Throw<DomainException>();
    }
}
