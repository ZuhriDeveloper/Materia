using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Stores;
using Materia.Domain.Stores.Events;

namespace Materia.Tests.Stores;

public class StoreParametersTests
{
    private static Store RegisteredStore()
    {
        var store = Store.Register("Toko Pusat", "S1", "owner-1");
        store.ClearDomainEvents();
        return store;
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateParameters_WithValidData_RaisesStoreParametersUpdatedAndSetsState()
    {
        var store = RegisteredStore();

        store.UpdateParameters("Jl. Merdeka 1", "+628123456789", 15.5m, "admin-1");

        var evt = store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreParametersUpdated>().Subject;

        evt.Address.Should().Be("Jl. Merdeka 1");
        evt.Phone.Should().Be("+628123456789");
        evt.MaxDeliveryDistanceKm.Should().Be(15.5m);
        evt.UpdatedBy.Should().Be("admin-1");
        evt.StoreId.Should().Be(store.Id);

        store.Address.Should().Be("Jl. Merdeka 1");
        store.Phone.Should().Be("+628123456789");
        store.MaxDeliveryDistanceKm.Should().Be(15.5m);
    }

    [Fact]
    public void UpdateParameters_Trims_AddressAndPhone()
    {
        var store = RegisteredStore();

        store.UpdateParameters("  Jl. Merdeka 1  ", "  +628123456789  ", 10m, "admin-1");

        store.Address.Should().Be("Jl. Merdeka 1");
        store.Phone.Should().Be("+628123456789");
    }

    // ── Guard: blank address ──────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateParameters_WithBlankAddress_ThrowsDomainException(string address)
    {
        var store = RegisteredStore();

        Action act = () => store.UpdateParameters(address, "+628123456789", 10m, "admin-1");

        act.Should().Throw<DomainException>().WithMessage("*address*");
    }

    // ── Guard: blank phone ────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateParameters_WithBlankPhone_ThrowsDomainException(string phone)
    {
        var store = RegisteredStore();

        Action act = () => store.UpdateParameters("Jl. Merdeka 1", phone, 10m, "admin-1");

        act.Should().Throw<DomainException>().WithMessage("*phone*");
    }

    // ── Guard: distance <= 0 ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public void UpdateParameters_WithNonPositiveDistance_ThrowsDomainException(decimal distance)
    {
        var store = RegisteredStore();

        Action act = () => store.UpdateParameters("Jl. Merdeka 1", "+628123456789", distance, "admin-1");

        act.Should().Throw<DomainException>().WithMessage("*distance*");
    }

    // ── Reconstitution ────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_WithParametersUpdated_RestoresParameterState()
    {
        var original = Store.Register("Toko", "S1", "owner-1");
        original.UpdateParameters("Jl. Merdeka 1", "+628123456789", 20m, "admin-1");

        var restored = Store.Reconstitute(original.DomainEvents.ToList());

        restored.Address.Should().Be("Jl. Merdeka 1");
        restored.Phone.Should().Be("+628123456789");
        restored.MaxDeliveryDistanceKm.Should().Be(20m);
        restored.DomainEvents.Should().BeEmpty();
    }
}
