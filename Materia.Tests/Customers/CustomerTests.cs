using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Customers;
using Materia.Domain.Customers.Events;

namespace Materia.Tests.Customers;

public class CustomerTests
{
    private static Coordinates AnyCoords() => new(1.234567m, 103.987654m);

    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Create_WithValidData_RaisesCustomerCreated()
    {
        var customer = Customer.Create("Budi Santoso", "08123456789", "budi@email.com", "user-1");

        var evt = customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreated>().Subject;

        evt.Name.Should().Be("Budi Santoso");
        evt.Phone.Should().Be("08123456789");
        evt.Email.Should().Be("budi@email.com");
        evt.CreatedBy.Should().Be("user-1");
        customer.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Customer.Create(name, "08123456789", null, "user-1");
        act.Should().Throw<DomainException>();
    }

    // â”€â”€ Add Address â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void AddAddress_FirstAddress_BecomesDefault()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        customer.ClearDomainEvents();

        var addrId = customer.AddAddress("Home", "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");

        var evt = customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerAddressAdded>().Subject;

        evt.IsDefault.Should().BeTrue();
        evt.AddressId.Should().Be(addrId);
        customer.Addresses.Should().HaveCount(1);
        customer.Addresses[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddAddress_SecondAddress_IsNotDefault()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        customer.AddAddress("Home", "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        customer.ClearDomainEvents();

        customer.AddAddress("Office", "Jl. B", "Jakarta", "DKI", null, AnyCoords(), "user-1");

        var evt = customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerAddressAdded>().Subject;
        evt.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void AddAddress_WhenInactive_ThrowsDomainException()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        customer.Deactivate("user-1");

        Action act = () => customer.AddAddress("Home", "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        act.Should().Throw<DomainException>();
    }

    // â”€â”€ Remove Address â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void RemoveAddress_NonDefault_Succeeds()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        customer.AddAddress("Home",   "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        var officeId = customer.AddAddress("Office", "Jl. B", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        customer.ClearDomainEvents();

        customer.RemoveAddress(officeId, "user-1");

        customer.Addresses.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveAddress_DefaultWhileOthersExist_ThrowsDomainException()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        var homeId = customer.AddAddress("Home",   "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        customer.AddAddress("Office", "Jl. B", "Jakarta", "DKI", null, AnyCoords(), "user-1");

        Action act = () => customer.RemoveAddress(homeId, "user-1");
        act.Should().Throw<DomainException>().WithMessage("*utama*");
    }

    // â”€â”€ Set Default â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void SetDefaultAddress_ChangesDefault()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        customer.AddAddress("Home",   "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        var officeId = customer.AddAddress("Office", "Jl. B", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        customer.ClearDomainEvents();

        customer.SetDefaultAddress(officeId, "user-1");

        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerDefaultAddressChanged>();
        customer.Addresses.First(a => a.Id == officeId).IsDefault.Should().BeTrue();
        customer.Addresses.First(a => a.Label == "Home").IsDefault.Should().BeFalse();
    }

    [Fact]
    public void SetDefaultAddress_AlreadyDefault_IsIdempotent()
    {
        var customer = Customer.Create("Budi", "08123456789", null, "user-1");
        var homeId = customer.AddAddress("Home", "Jl. A", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        customer.ClearDomainEvents();

        customer.SetDefaultAddress(homeId, "user-1");

        customer.DomainEvents.Should().BeEmpty();
    }

    // â”€â”€ Reconstitute â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Reconstitute_FromEvents_RestoresFullState()
    {
        var original = Customer.Create("Budi", "08123456789", "budi@x.com", "user-1");
        var homeId = original.AddAddress("Home", "Jl. A", "Jakarta", "DKI", "12345", AnyCoords(), "user-1");
        var officeId = original.AddAddress("Office", "Jl. B", "Jakarta", "DKI", null, AnyCoords(), "user-1");
        original.SetDefaultAddress(officeId, "user-1");
        original.RemoveAddress(homeId, "user-1");
        original.Deactivate("user-2");

        var reconstituted = Customer.Reconstitute(original.DomainEvents);

        reconstituted.Name.Should().Be("Budi");
        reconstituted.Phone.Value.Should().Be("08123456789");
        reconstituted.IsActive.Should().BeFalse();
        reconstituted.Addresses.Should().HaveCount(1);
        reconstituted.Addresses[0].Label.Should().Be("Office");
        reconstituted.Addresses[0].IsDefault.Should().BeTrue();
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}

