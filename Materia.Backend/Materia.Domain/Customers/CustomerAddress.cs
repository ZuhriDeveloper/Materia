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
    /// <summary>Kelurahan / Desa.</summary>
    public string?     Subdistrict { get; private set; }
    /// <summary>Kecamatan.</summary>
    public string?     District    { get; private set; }
    /// <summary>Kabupaten / Kota.</summary>
    public string      City        { get; private set; }
    public string       Province    { get; private set; }
    public string?      PostalCode  { get; private set; }
    /// <summary>Optional map pin. Null when the address has not been located on the map.</summary>
    public Coordinates? Coordinates { get; private set; }
    public bool         IsDefault   { get; internal set; }

    internal CustomerAddress(
        AddressId    id,
        string       label,
        string       street,
        string       city,
        string       province,
        string?      postalCode,
        Coordinates? coordinates,
        bool         isDefault,
        string?      subdistrict = null,
        string?      district    = null)
    {
        Id          = id;
        Label       = label;
        Street      = street;
        Subdistrict = subdistrict;
        District    = district;
        City        = city;
        Province    = province;
        PostalCode  = postalCode;
        Coordinates = coordinates;
        IsDefault   = isDefault;
    }

    internal void Update(
        string       label,
        string       street,
        string       city,
        string       province,
        string?      postalCode,
        Coordinates? coordinates,
        string?      subdistrict = null,
        string?      district    = null)
    {
        Label       = label;
        Street      = street;
        Subdistrict = subdistrict;
        District    = district;
        City        = city;
        Province    = province;
        PostalCode  = postalCode;
        Coordinates = coordinates;
    }
}
