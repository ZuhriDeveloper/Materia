using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Stores;
using Materia.Domain.Stores.Events;

namespace Materia.Tests.Stores;

public class StoreTests
{
    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public void Register_WithValidData_RaisesStoreRegisteredAndSetsState()
    {
        var store = Store.Register("Toko Pusat", "S1", "owner-1");

        var evt = store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreRegistered>().Subject;

        evt.Name.Should().Be("Toko Pusat");
        evt.Code.Should().Be("S1");
        evt.RegisteredBy.Should().Be("owner-1");

        store.IsActive.Should().BeTrue();
        store.Name.Should().Be("Toko Pusat");
        store.Code.Should().Be("S1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Store.Register(name, "S1", "owner-1");

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankCode_ThrowsDomainException(string code)
    {
        Action act = () => Store.Register("Toko Pusat", code, "owner-1");

        act.Should().Throw<DomainException>().WithMessage("*code*");
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public void Rename_WithNewName_RaisesStoreRenamed()
    {
        var store = Store.Register("Toko", "S1", "owner-1");
        store.ClearDomainEvents();

        store.Rename("Toko Cabang", "owner-2");

        store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreRenamed>();
        store.Name.Should().Be("Toko Cabang");
    }

    [Fact]
    public void Rename_WithSameName_RaisesNoEvent()
    {
        var store = Store.Register("Toko Cabang", "S1", "owner-1");
        store.ClearDomainEvents();

        store.Rename("Toko Cabang", "owner-2");

        store.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithBlankName_ThrowsDomainException(string name)
    {
        var store = Store.Register("Toko", "S1", "owner-1");
        store.ClearDomainEvents();

        Action act = () => store.Rename(name, "owner-2");

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    // ── Deactivate / Activate ─────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_RaisesStoreDeactivatedAndSetsInactive()
    {
        var store = Store.Register("Toko", "S1", "owner-1");
        store.ClearDomainEvents();

        store.Deactivate("owner-1");

        store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreDeactivated>();
        store.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var store = Store.Register("Toko", "S1", "owner-1");
        store.Deactivate("owner-1");

        Action act = () => store.Deactivate("owner-1");

        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_WhenInactive_RaisesStoreActivatedAndSetsActive()
    {
        var store = Store.Register("Toko", "S1", "owner-1");
        store.Deactivate("owner-1");
        store.ClearDomainEvents();

        store.Activate("owner-2");

        store.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StoreActivated>();
        store.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsDomainException()
    {
        var store = Store.Register("Toko", "S1", "owner-1");

        Action act = () => store.Activate("owner-1");

        act.Should().Throw<DomainException>().WithMessage("*already active*");
    }

    // ── Event Sourcing Reconstruction ─────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromEvents_RestoresFullStateWithNoUncommittedEvents()
    {
        var original = Store.Register("Toko", "S1", "owner-1");
        original.Rename("Toko Cabang", "owner-2");
        original.Deactivate("owner-1");

        var restored = Store.Reconstitute(original.DomainEvents.ToList());

        restored.Id.Should().Be(original.Id);
        restored.Name.Should().Be("Toko Cabang");
        restored.Code.Should().Be("S1");
        restored.IsActive.Should().BeFalse();
        restored.DomainEvents.Should().BeEmpty();
    }
}
