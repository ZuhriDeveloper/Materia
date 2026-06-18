using FluentAssertions;
using Materia.Application.Commands.Purchasing.ReceivePurchaseOrder;
using Materia.Application.Contracts.Inventory;
using Materia.Application.Contracts.Purchasing;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class ReceivePurchaseOrderHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakePurchaseOrderRepository : IPurchaseOrderRepository
    {
        private PurchaseOrder? _stored;
        public PurchaseOrder? Saved { get; private set; }

        public void Seed(PurchaseOrder po) => _stored = po;

        public Task<PurchaseOrder?> GetByIdAsync(PurchaseOrderId id, CancellationToken ct = default)
            => Task.FromResult(_stored?.Id == id ? _stored : null);

        public Task SaveAsync(PurchaseOrder po, CancellationToken ct = default)
        {
            Saved = po;
            po.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStockRepository : IStockRepository
    {
        private readonly List<Stock> _stored = [];
        public List<Stock> Saved { get; } = [];

        public void Seed(Stock stock) => _stored.Add(stock);

        public Task<Stock?> GetAsync(
            ProductId productId, VariantId? variantId = null, CancellationToken ct = default)
            => Task.FromResult(_stored.FirstOrDefault(s =>
                s.ProductId == productId && NullableVariantEquals(s.VariantId, variantId)));

        public Task SaveAsync(Stock stock, CancellationToken ct = default)
        {
            if (!Saved.Contains(stock)) Saved.Add(stock);
            stock.ClearDomainEvents();
            return Task.CompletedTask;
        }

        private static bool NullableVariantEquals(VariantId? a, VariantId? b)
            => (a is null && b is null) || (a is not null && b is not null && a.Value == b.Value);
    }

    // Catalog sync is skipped unless UpdateCatalogOnReceipt is set, so a no-op supplier repo suffices.
    private sealed class FakeSupplierRepository : ISupplierRepository
    {
        public Task<Supplier?> GetByIdAsync(SupplierId id, CancellationToken ct = default)
            => Task.FromResult<Supplier?>(null);

        public Task SaveAsync(Supplier supplier, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        ReceivePurchaseOrderCommandHandler handler,
        FakePurchaseOrderRepository poRepo,
        FakeStockRepository stockRepo)
    BuildHandler()
    {
        var poRepo = new FakePurchaseOrderRepository();
        var stockRepo = new FakeStockRepository();
        var handler = new ReceivePurchaseOrderCommandHandler(
            poRepo, stockRepo, new FakeSupplierRepository());
        return (handler, poRepo, stockRepo);
    }

    private static PurchaseOrder ConfirmedPo(ProductId productId, decimal orderedQty = 10m)
    {
        var po = PurchaseOrder.Create(
            SupplierId.New(), [(productId, orderedQty, 50_000m, "pcs")], "user1");
        po.Confirm("user1");
        po.ClearDomainEvents();
        return po;
    }

    private static Stock SeededStock(ProductId productId, VariantId? variantId = null)
    {
        var stock = Stock.Initialize(productId, "pcs", "system", variantId);
        stock.ClearDomainEvents();
        return stock;
    }

    // ── Per-variant routing ───────────────────────────────────────────────────

    [Fact]
    public async Task Receive_WithVariantId_ReconcilesVariantStock_NotProductLevel()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var variantId = VariantId.New();

        var po = ConfirmedPo(productId);
        poRepo.Seed(po);

        var productStock = SeededStock(productId);
        var variantStock = SeededStock(productId, variantId);
        stockRepo.Seed(productStock);
        stockRepo.Seed(variantStock);

        await handler.HandleAsync(new ReceivePurchaseOrderCommand(
            po.Id.Value,
            [new ReceivePurchaseOrderLineInput(productId.Value, 5m, variantId.Value)],
            "warehouse"));

        variantStock.Quantity.Should().Be(5m);
        productStock.Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task Receive_WithoutVariantId_ReconcilesProductLevelStock()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var po = ConfirmedPo(productId);
        poRepo.Seed(po);

        var productStock = SeededStock(productId);
        var variantStock = SeededStock(productId, VariantId.New());
        stockRepo.Seed(productStock);
        stockRepo.Seed(variantStock);

        await handler.HandleAsync(new ReceivePurchaseOrderCommand(
            po.Id.Value,
            [new ReceivePurchaseOrderLineInput(productId.Value, 5m)],
            "warehouse"));

        productStock.Quantity.Should().Be(5m);
        variantStock.Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task Receive_WithUnknownVariant_ThrowsDomainException()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var po = ConfirmedPo(productId);
        poRepo.Seed(po);
        stockRepo.Seed(SeededStock(productId)); // only product-level; no variant bucket

        Func<Task> act = () => handler.HandleAsync(new ReceivePurchaseOrderCommand(
            po.Id.Value,
            [new ReceivePurchaseOrderLineInput(productId.Value, 5m, VariantId.New().Value)],
            "warehouse"));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*variant*");
    }

    [Fact]
    public async Task Receive_WithoutVariantId_AndNoStock_InitializesProductLevel()
    {
        var (handler, poRepo, stockRepo) = BuildHandler();
        var productId = ProductId.New();
        var po = ConfirmedPo(productId);
        poRepo.Seed(po);
        // No stock seeded — first-ever receipt for this product.

        await handler.HandleAsync(new ReceivePurchaseOrderCommand(
            po.Id.Value,
            [new ReceivePurchaseOrderLineInput(productId.Value, 7m)],
            "warehouse"));

        var saved = stockRepo.Saved.Should().ContainSingle().Subject;
        saved.ProductId.Should().Be(productId);
        saved.VariantId.Should().BeNull();
        saved.Quantity.Should().Be(7m);
    }
}
