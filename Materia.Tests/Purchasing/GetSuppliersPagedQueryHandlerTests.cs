using FluentAssertions;
using FluentValidation;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Purchasing;

namespace Materia.Tests.Purchasing;

public class GetSuppliersPagedQueryHandlerTests
{
    // ── Fake repository ───────────────────────────────────────────────────────

    private sealed class FakeSupplierQueryRepository : ISupplierQueryRepository
    {
        private readonly List<SupplierDto> _data;

        public FakeSupplierQueryRepository(IEnumerable<SupplierDto>? data = null)
            => _data = data?.ToList() ?? [];

        public Task<PagedResult<SupplierDto>> SearchAsync(
            string? search, bool activeOnly, int page, int pageSize, CancellationToken ct = default)
        {
            var q = _data.AsEnumerable();

            if (activeOnly)
                q = q.Where(s => s.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(s =>
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (s.ContactPhone  != null && s.ContactPhone.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.SalesmanName  != null && s.SalesmanName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.SalesmanPhone != null && s.SalesmanPhone.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Description   != null && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));

            var ordered = q.OrderBy(s => s.Name).ToList();
            var total   = ordered.Count;
            var items   = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult(new PagedResult<SupplierDto>(items, total, page, pageSize));
        }

        // ── other interface members not used by the paged handler ─────────────

        public Task<IReadOnlyList<SupplierDto>> GetAllAsync(bool activeOnly, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<SupplierDto?> GetByIdAsync(Guid supplierId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<SupplierBestPriceResult?> GetBestPriceForProductAsync(Guid productId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SupplierDto MakeSupplier(
        string name, string? phone = null, bool active = true,
        string? description = null, string? salesmanName = null, string? salesmanPhone = null)
        => new(Guid.NewGuid(), name, phone, description, salesmanName, salesmanPhone, active, []);

    private static GetSuppliersPagedQueryHandler MakeHandler(IEnumerable<SupplierDto>? data = null)
        => new(new FakeSupplierQueryRepository(data));

    // ── Search tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ByPartialName_ReturnsMatchingSuppliers()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan"),
            MakeSupplier("CV Maju Abadi"),
            MakeSupplier("Toko Semen Jaya"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("jaya"), default);

        result.Items.Should().HaveCount(2);
        result.Items.Select(s => s.Name).Should().Contain("PT Jaya Bahan");
        result.Items.Select(s => s.Name).Should().Contain("Toko Semen Jaya");
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var data = new[] { MakeSupplier("PT Jaya Bahan") };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("JAYA"), default);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_ByPhone_ReturnsMatchingSuppliers()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan", "0812345678"),
            MakeSupplier("CV Maju Abadi", "0899000000"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("0812"), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("PT Jaya Bahan");
    }

    [Fact]
    public async Task Search_BySalesmanName_ReturnsMatchingSuppliers()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan", salesmanName: "Budi Santoso"),
            MakeSupplier("CV Maju Abadi", salesmanName: "Andi Wijaya"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("budi"), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("PT Jaya Bahan");
    }

    [Fact]
    public async Task Search_BySalesmanPhone_ReturnsMatchingSuppliers()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan", salesmanPhone: "0855111222"),
            MakeSupplier("CV Maju Abadi", salesmanPhone: "0866333444"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("0855"), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("PT Jaya Bahan");
    }

    [Fact]
    public async Task Search_ByDescription_ReturnsMatchingSuppliers()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan", description: "Pemasok semen dan pasir"),
            MakeSupplier("CV Maju Abadi", description: "Pemasok cat tembok"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("pasir"), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("PT Jaya Bahan");
    }

    [Fact]
    public async Task Search_WithNoMatch_ReturnsEmptyList()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan"),
            MakeSupplier("CV Maju Abadi"),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(new GetSuppliersPagedQuery("ZZZNOMATCH"), default);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── Paging tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Paging_Page1Of2_ReturnsFirstHalf()
    {
        var data = Enumerable.Range(1, 5)
            .Select(i => MakeSupplier($"Supplier {i:D2}"))
            .ToArray();
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(
            new GetSuppliersPagedQuery(Page: 1, PageSize: 3), default);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.Items[0].Name.Should().Be("Supplier 01");
    }

    [Fact]
    public async Task Paging_Page2_ReturnsRemainingItems()
    {
        var data = Enumerable.Range(1, 5)
            .Select(i => MakeSupplier($"Supplier {i:D2}"))
            .ToArray();
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(
            new GetSuppliersPagedQuery(Page: 2, PageSize: 3), default);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Items[0].Name.Should().Be("Supplier 04");
        result.Items[1].Name.Should().Be("Supplier 05");
    }

    [Fact]
    public async Task TotalCount_IsCorrectRegardlessOfPage()
    {
        var data = Enumerable.Range(1, 7)
            .Select(i => MakeSupplier($"Supplier {i:D2}"))
            .ToArray();
        var handler = MakeHandler(data);

        var page1 = await handler.HandleAsync(new GetSuppliersPagedQuery(Page: 1, PageSize: 5), default);
        var page2 = await handler.HandleAsync(new GetSuppliersPagedQuery(Page: 2, PageSize: 5), default);

        page1.TotalCount.Should().Be(7);
        page2.TotalCount.Should().Be(7);
    }

    // ── Combined filter tests ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchAndActiveOnly_CombinedFilter_Works()
    {
        var data = new[]
        {
            MakeSupplier("PT Jaya Bahan", active: true),
            MakeSupplier("CV Jaya Makmur", active: false),
            MakeSupplier("CV Maju Abadi", active: true),
        };
        var handler = MakeHandler(data);

        var result = await handler.HandleAsync(
            new GetSuppliersPagedQuery(Search: "jaya", ActiveOnly: true), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("PT Jaya Bahan");
    }

    // ── Validator tests ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    public async Task Validator_PageBelowOne_IsInvalid(int page, int pageSize)
    {
        var validator = new GetSuppliersPagedQueryValidator();
        var result = await validator.ValidateAsync(new GetSuppliersPagedQuery(Page: page, PageSize: pageSize));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public async Task Validator_PageSizeOutOfRange_IsInvalid(int page, int pageSize)
    {
        var validator = new GetSuppliersPagedQueryValidator();
        var result = await validator.ValidateAsync(new GetSuppliersPagedQuery(Page: page, PageSize: pageSize));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, 100)]
    public async Task Validator_ValidPageAndPageSize_IsValid(int page, int pageSize)
    {
        var validator = new GetSuppliersPagedQueryValidator();
        var result = await validator.ValidateAsync(new GetSuppliersPagedQuery(Page: page, PageSize: pageSize));
        result.IsValid.Should().BeTrue();
    }
}
