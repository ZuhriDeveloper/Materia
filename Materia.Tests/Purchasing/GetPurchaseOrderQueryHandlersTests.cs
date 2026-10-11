using FluentAssertions;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Application.DTOs.Inventory;
using Materia.Application.Queries.Purchasing;

namespace Materia.Tests.Purchasing;

public class GetPurchaseOrderQueryHandlersTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakePurchaseOrderQueryRepository(IEnumerable<PurchaseOrderDto> data)
        : IPurchaseOrderQueryRepository
    {
        private readonly List<PurchaseOrderDto> _data = data.ToList();

        public Task<IReadOnlyList<PurchaseOrderDto>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PurchaseOrderDto>>(_data);

        public Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_data.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeProductQueryRepository(IReadOnlyDictionary<Guid, string> names)
        : IProductQueryRepository
    {
        public List<IReadOnlyCollection<Guid>> NameLookups { get; } = [];

        public Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        {
            NameLookups.Add(ids);
            IReadOnlyDictionary<Guid, string> found = ids
                .Where(names.ContainsKey)
                .ToDictionary(id => id, id => names[id]);
            return Task.FromResult(found);
        }

        // ── other interface members not used by the PO query handlers ─────────

        public Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<PagedResult<ProductDto>> GetPagedAsync(
            int page, int pageSize, bool? isActive, string? search = null, Guid? categoryId = null,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<ProductDto>> GetAllAsync(
            bool? isActive, string? search = null, Guid? categoryId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludeId = null, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PurchaseOrderLineDto MakeLine(Guid productId)
        => new(productId, null, 10m, 0m, 0m, 50_000m, "sak", 50_000m, []);

    private static PurchaseOrderDto MakeOrder(params Guid[] productIds)
        => new(Guid.NewGuid(), Guid.NewGuid(), "PT Jaya Bahan", "Draft",
               productIds.Select(MakeLine).ToList(),
               "admin", DateTime.UtcNow, null, null, null, null);

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_FillsProductNamesOnLines()
    {
        var semen = Guid.NewGuid();
        var pasir = Guid.NewGuid();
        var order = MakeOrder(semen, pasir);
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string>
        {
            [semen] = "Semen Gresik 40kg",
            [pasir] = "Pasir Lumajang",
        });
        var handler = new GetPurchaseOrderByIdQueryHandler(
            new FakePurchaseOrderQueryRepository([order]), products);

        var result = await handler.HandleAsync(order.Id);

        result.Should().NotBeNull();
        result!.Lines.Select(l => l.ProductName)
            .Should().Equal("Semen Gresik 40kg", "Pasir Lumajang");
    }

    [Fact]
    public async Task GetById_UnknownProduct_LeavesNameNull()
    {
        var known   = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        var order = MakeOrder(known, unknown);
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string>
        {
            [known] = "Semen Gresik 40kg",
        });
        var handler = new GetPurchaseOrderByIdQueryHandler(
            new FakePurchaseOrderQueryRepository([order]), products);

        var result = await handler.HandleAsync(order.Id);

        result!.Lines.Single(l => l.ProductId == unknown).ProductName.Should().BeNull();
        result.Lines.Single(l => l.ProductId == known).ProductName.Should().Be("Semen Gresik 40kg");
    }

    [Fact]
    public async Task GetById_PreservesOtherLineFields()
    {
        var semen = Guid.NewGuid();
        var order = MakeOrder(semen);
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string> { [semen] = "Semen" });
        var handler = new GetPurchaseOrderByIdQueryHandler(
            new FakePurchaseOrderQueryRepository([order]), products);

        var result = await handler.HandleAsync(order.Id);

        result!.Lines[0].Should().BeEquivalentTo(order.Lines[0] with { ProductName = "Semen" });
    }

    [Fact]
    public async Task GetById_MissingOrder_ReturnsNullWithoutLookingUpNames()
    {
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string>());
        var handler = new GetPurchaseOrderByIdQueryHandler(
            new FakePurchaseOrderQueryRepository([]), products);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
        products.NameLookups.Should().BeEmpty();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_FillsProductNamesWithSingleLookupOfDistinctIds()
    {
        var semen = Guid.NewGuid();
        var pasir = Guid.NewGuid();
        var orders = new[] { MakeOrder(semen, pasir), MakeOrder(semen) };
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string>
        {
            [semen] = "Semen Gresik 40kg",
            [pasir] = "Pasir Lumajang",
        });
        var handler = new GetPurchaseOrdersQueryHandler(
            new FakePurchaseOrderQueryRepository(orders), products);

        var result = await handler.HandleAsync();

        result.SelectMany(o => o.Lines).Should().OnlyContain(l => l.ProductName != null);
        result[1].Lines[0].ProductName.Should().Be("Semen Gresik 40kg");
        products.NameLookups.Should().ContainSingle()
            .Which.Should().BeEquivalentTo([semen, pasir]);
    }

    [Fact]
    public async Task GetAll_NoOrders_SkipsNameLookup()
    {
        var products = new FakeProductQueryRepository(new Dictionary<Guid, string>());
        var handler = new GetPurchaseOrdersQueryHandler(
            new FakePurchaseOrderQueryRepository([]), products);

        var result = await handler.HandleAsync();

        result.Should().BeEmpty();
        products.NameLookups.Should().BeEmpty();
    }
}
