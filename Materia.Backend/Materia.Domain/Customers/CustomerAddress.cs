namespace Materia.Domain.Customers;

/// <summary>
/// A delivery location owned by the Customer aggregate.
/// Has identity (AddressId) but is not an aggregate root.
/// </summary>
public sealed class CustomerAddress
{
    public AddressId   Id          { get; }
    public string      Label       { get; private set; }
    public string      Street      { get; private set; }
    public string      City        { get; private set; }
    public string      Province    { get; private set; }
    public string?     PostalCode  { get; private set; }
    public Coordinates Coordinates { get; private set; }
    public bool        IsDefault   { get; internal set; }

    internal CustomerAddress(
        AddressId   id,
        string      label,
        string      street,
        string      city,
        string      province,
        string?     postalCode,
        Coordinates coordinates,
        bool        isDefault)
    {
        Id          = id;
        Label       = label;
        Street      = street;
        City        = city;
        Province    = province;
        PostalCode  = postalCode;
        Coordinates = coordinates;
        IsDefault   = isDefault;
    }

    internal void Update(
        string      label,
        string      street,
        string      city,
        string      province,
        string?     postalCode,
        Coordinates coordinates)
    {
        Label       = label;
        Street      = street;
        City        = city;
        Province    = province;
        PostalCode  = postalCode;
        Coordinates = coordinates;
    }
}
