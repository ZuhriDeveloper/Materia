namespace Materia.Domain.Customers;

/// <summary>
/// Haversine great-circle distance. Accurate to &lt;0.5% for distances under 2 000 km.
/// </summary>
public static class DistanceCalculator
{
    private const double EarthRadiusKm = 6371.0;

    /// <returns>Distance in kilometres.</returns>
    public static double BetweenKm(Coordinates from, Coordinates to)
    {
        var lat1 = ToRad((double)from.Latitude);
        var lat2 = ToRad((double)to.Latitude);
        var dLat = ToRad((double)(to.Latitude  - from.Latitude));
        var dLon = ToRad((double)(to.Longitude - from.Longitude));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <returns>Distance in metres.</returns>
    public static double BetweenMetres(Coordinates from, Coordinates to)
        => BetweenKm(from, to) * 1000;

    public static DeliveryTier GetDeliveryTier(Coordinates store, Coordinates destination)
        => BetweenKm(store, destination) switch
        {
            <= 5   => DeliveryTier.Local,
            <= 20  => DeliveryTier.City,
            <= 100 => DeliveryTier.Regional,
            _      => DeliveryTier.LongDistance,
        };

    private static double ToRad(double deg) => deg * Math.PI / 180;
}

public enum DeliveryTier { Local, City, Regional, LongDistance }
