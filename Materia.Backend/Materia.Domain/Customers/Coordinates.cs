using Materia.Domain.Common;

namespace Materia.Domain.Customers;

public record Coordinates
{
    public decimal Latitude  { get; }
    public decimal Longitude { get; }

    public Coordinates(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
            throw new DomainException($"Latitude must be between -90 and 90. Got: {latitude}");
        if (longitude is < -180m or > 180m)
            throw new DomainException($"Longitude must be between -180 and 180. Got: {longitude}");

        Latitude  = Math.Round(latitude,  8);   // ~1 mm precision
        Longitude = Math.Round(longitude, 8);
    }

    public override string ToString() => $"{Latitude}, {Longitude}";
}
