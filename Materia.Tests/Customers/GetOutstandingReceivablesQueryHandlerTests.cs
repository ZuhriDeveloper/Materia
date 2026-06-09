using FluentAssertions;
using Materia.Application.Contracts.Customers;
using Materia.Application.DTOs.Customers;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Customers;

namespace Materia.Tests.Customers;

public class GetOutstandingReceivablesQueryHandlerTests
{
    /// <summary>Captures the paging args the handler forwards to the repository.</summary>
    private sealed class CapturingRepository : ICustomerQueryRepository
    {
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        public Task<PagedResult<ReceivableSummaryDto>> GetOutstandingReceivablesAsync(
            int page, int pageSize, string? search, CancellationToken ct = default)
        {
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(
                new PagedResult<ReceivableSummaryDto>([], 0, page, pageSize));
        }

        // Unused by this handler.
        public Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<CustomerDto>> GetPagedAsync(
            int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<NearbyCustomerDto>> GetNearbyAsync(
            decimal latitude, decimal longitude, double radiusKm, int maxResults, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeId = null, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    [Theory]
    [InlineData(0, 1)]      // page below 1 → 1
    [InlineData(-5, 1)]     // negative page → 1 (no negative OFFSET)
    [InlineData(3, 3)]      // valid page passes through
    public async Task ClampsPageToAtLeastOne(int requestedPage, int expectedPage)
    {
        var repo = new CapturingRepository();
        var handler = new GetOutstandingReceivablesQueryHandler(repo);

        await handler.HandleAsync(new GetOutstandingReceivablesQuery(requestedPage, 20, null));

        repo.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 20)]     // below 1 → default 20
    [InlineData(-1, 20)]    // negative → default 20
    [InlineData(100_000, 20)] // above max → default 20 (cannot pull the whole debtor list)
    [InlineData(50, 50)]    // within bounds passes through
    public async Task ClampsPageSizeToSafeBounds(int requestedPageSize, int expectedPageSize)
    {
        var repo = new CapturingRepository();
        var handler = new GetOutstandingReceivablesQueryHandler(repo);

        await handler.HandleAsync(new GetOutstandingReceivablesQuery(1, requestedPageSize, null));

        repo.PageSize.Should().Be(expectedPageSize);
    }
}
