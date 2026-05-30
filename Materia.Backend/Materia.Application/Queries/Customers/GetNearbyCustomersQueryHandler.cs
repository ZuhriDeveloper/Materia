using Materia.Application.Contracts.Customers;
using Materia.Application.DTOs.Customers;
using Materia.Domain.Customers;

namespace Materia.Application.Queries.Customers;

public record GetNearbyCustomersQuery(
    decimal Latitude,
    decimal Longitude,
    double  RadiusKm   = 20,
    int     MaxResults = 50);

public class GetNearbyCustomersQueryHandler(ICustomerQueryRepository repository)
{
    public async Task<IReadOnlyList<NearbyCustomerDto>> HandleAsync(
        GetNearbyCustomersQuery query, CancellationToken ct = default)
    {
        var candidates = await repository.GetNearbyAsync(
            query.Latitude, query.Longitude, query.RadiusKm, query.MaxResults, ct);

        var origin = new Coordinates(query.Latitude, query.Longitude);

        return candidates
            .Select(c => c with
            {
                DistanceKm = DistanceCalculator.BetweenKm(
                    origin, new Coordinates(c.Latitude, c.Longitude))
            })
            .Where(c => c.DistanceKm <= query.RadiusKm)
            .OrderBy(c => c.DistanceKm)
            .ToList();
    }
}
