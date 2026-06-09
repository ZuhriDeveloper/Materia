using Materia.Application.Contracts.Customers;
using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;
using Materia.Infrastructure.Persistence;
using Materia.Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Customers;

public class CustomerQueryRepository(AppDbContext context) : ICustomerQueryRepository
{
    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await context.CustomerReadModels
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return c is null ? null : Map(c);
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(
        int page, int pageSize, string? search, bool? isActive,
        CancellationToken ct = default)
    {
        var query = context.CustomerReadModels
            .AsNoTracking()
            .Include(x => x.Addresses)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                EF.Functions.ILike(x.Name,  $"%{search}%") ||
                EF.Functions.ILike(x.Phone, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    /// <summary>
    /// Bounding-box SQL pre-filter → Haversine refinement done in the query handler.
    /// Upgrade path: add PostGIS GIST index for millions of rows.
    /// </summary>
    public async Task<IReadOnlyList<NearbyCustomerDto>> GetNearbyAsync(
        decimal latitude, decimal longitude, double radiusKm, int maxResults,
        CancellationToken ct = default)
    {
        var latDelta = (decimal)(radiusKm / 111.0);
        var lonDelta = (decimal)(radiusKm /
            (111.0 * Math.Cos((double)latitude * Math.PI / 180)));

        var rows = await context.CustomerAddressReadModels
            .AsNoTracking()
            .Where(a =>
                a.Latitude.HasValue && a.Longitude.HasValue &&
                a.Latitude  >= latitude  - latDelta &&
                a.Latitude  <= latitude  + latDelta &&
                a.Longitude >= longitude - lonDelta &&
                a.Longitude <= longitude + lonDelta)
            .Join(context.CustomerReadModels.Where(c => c.IsActive),
                a => a.CustomerId, c => c.Id,
                (a, c) => new { Address = a, Customer = c })
            .Take(maxResults * 3)
            .ToListAsync(ct);

        return rows.Select(x => new NearbyCustomerDto(
            x.Customer.Id,
            x.Customer.Name,
            x.Customer.Phone,
            x.Address.Id,
            x.Address.Label,
            $"{x.Address.Street}, {x.Address.City}, {x.Address.Province}",
            x.Address.Latitude!.Value,
            x.Address.Longitude!.Value,
            0d   // DistanceKm filled in by GetNearbyCustomersQueryHandler
        )).ToList();
    }

    public Task<bool> ExistsByPhoneAsync(
        string phone, Guid? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = phone.Trim();
        return excludeId.HasValue
            ? context.CustomerReadModels
                .AnyAsync(c => c.Phone == trimmed && c.Id != excludeId.Value, ct)
            : context.CustomerReadModels
                .AnyAsync(c => c.Phone == trimmed, ct);
    }

    public async Task<PagedResult<ReceivableSummaryDto>> GetOutstandingReceivablesAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = context.CustomerReadModels
            .AsNoTracking()
            .Where(c => c.OutstandingDebt > 0);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                EF.Functions.ILike(c.Name,  $"%{search}%") ||
                EF.Functions.ILike(c.Phone, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.OutstandingDebt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ReceivableSummaryDto(c.Id, c.Name, c.Phone, c.OutstandingDebt))
            .ToListAsync(ct);

        return new PagedResult<ReceivableSummaryDto>(items, total, page, pageSize);
    }

    private static CustomerDto Map(CustomerReadModel c) => new(
        c.Id, c.Name, c.Phone, c.Email, c.IsActive, c.OutstandingDebt,
        c.CreatedBy, c.CreatedAt, c.UpdatedBy, c.UpdatedAt,
        c.Addresses
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Label)
            .Select(a => new CustomerAddressDto(
                a.Id, a.Label, a.Street, a.City, a.Province,
                a.PostalCode, a.Latitude, a.Longitude, a.IsDefault,
                a.Subdistrict, a.District))
            .ToList());
}
